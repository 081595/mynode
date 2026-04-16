using Microsoft.Data.Sqlite;
using TeacherAppointment.Application.Abstractions.Persistence;

namespace TeacherAppointment.Infrastructure.Persistence;

public sealed class SqliteAppointmentResponseRepository : IAppointmentResponseRepository
{
    private readonly ISqliteConnectionFactory _connectionFactory;

    public SqliteAppointmentResponseRepository(ISqliteConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<IReadOnlyList<AppointmentResponseSummary>> GetForTeacherAsync(
        int year,
        string employeeNo,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
SELECT yr, empl_no, appo_doc_yy, appo_doc_ch, appo_doc_seq, file_name,
       resp_status, download_count, remark, update_date
FROM teach_appo_resp
WHERE yr = $yr AND empl_no = $emplNo
ORDER BY appo_doc_yy DESC, appo_doc_ch, appo_doc_seq;
""";
    EnsureNoPdfContentInSummaryQuery(command.CommandText);
        command.Parameters.AddWithValue("$yr", year);
        command.Parameters.AddWithValue("$emplNo", employeeNo);

        return await ReadSummariesAsync(command, cancellationToken);
    }

    public async Task<IReadOnlyList<AppointmentResponseSummary>> GetForAdminAsync(
        int year,
        string? employeeNo,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();

        if (string.IsNullOrWhiteSpace(employeeNo))
        {
            command.CommandText = """
SELECT yr, empl_no, appo_doc_yy, appo_doc_ch, appo_doc_seq, file_name,
       resp_status, download_count, remark, update_date
FROM teach_appo_resp
WHERE yr = $yr
ORDER BY empl_no, appo_doc_yy DESC, appo_doc_ch, appo_doc_seq;
""";
            EnsureNoPdfContentInSummaryQuery(command.CommandText);
            command.Parameters.AddWithValue("$yr", year);
        }
        else
        {
            command.CommandText = """
SELECT yr, empl_no, appo_doc_yy, appo_doc_ch, appo_doc_seq, file_name,
       resp_status, download_count, remark, update_date
FROM teach_appo_resp
WHERE yr = $yr AND empl_no = $emplNo
ORDER BY appo_doc_yy DESC, appo_doc_ch, appo_doc_seq;
""";
            EnsureNoPdfContentInSummaryQuery(command.CommandText);
            command.Parameters.AddWithValue("$yr", year);
            command.Parameters.AddWithValue("$emplNo", employeeNo);
        }

        return await ReadSummariesAsync(command, cancellationToken);
    }

    public async Task<AppointmentPdfPayload?> GetPdfAsync(
        AppointmentDocumentKey key,
        bool incrementDownloadCount,
        DateTime nowUtc,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var tx = connection.BeginTransaction();

        if (incrementDownloadCount)
        {
            await using var update = connection.CreateCommand();
            update.Transaction = tx;
            update.CommandText = """
UPDATE teach_appo_resp
SET download_count = download_count + 1,
    update_date = $updateDate
WHERE yr = $yr
  AND empl_no = $emplNo
  AND appo_doc_yy = $docYear
  AND appo_doc_ch = $docType
  AND appo_doc_seq = $docSeq;
""";
            BindKey(update, key);
            update.Parameters.AddWithValue("$updateDate", nowUtc.ToString("O"));
            await update.ExecuteNonQueryAsync(cancellationToken);
        }

        await using var read = connection.CreateCommand();
        read.Transaction = tx;
        read.CommandText = """
SELECT file_name, pdf_content, download_count, resp_status, update_date
FROM teach_appo_resp
WHERE yr = $yr
  AND empl_no = $emplNo
  AND appo_doc_yy = $docYear
  AND appo_doc_ch = $docType
  AND appo_doc_seq = $docSeq
LIMIT 1;
""";
        BindKey(read, key);

        await using var reader = await read.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            await tx.CommitAsync(cancellationToken);
            return null;
        }

        var fileName = reader.IsDBNull(0) ? "appointment.pdf" : reader.GetString(0);
        var content = (byte[])reader[1];
        var downloadCount = reader.GetInt32(2);
        var responded = reader.GetInt32(3) == 1;
        var updatedAtUtc = ParseDateTime(reader.GetString(4));

        await tx.CommitAsync(cancellationToken);
        return new AppointmentPdfPayload(key, fileName, content, downloadCount, responded, updatedAtUtc);
    }

    public async Task<AppointmentCompletionResult> MarkResponseCompletedAsync(
        AppointmentDocumentKey key,
        DateTime nowUtc,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var tx = connection.BeginTransaction();

        await using var update = connection.CreateCommand();
        update.Transaction = tx;
        update.CommandText = """
UPDATE teach_appo_resp
SET resp_status = 1,
    update_date = $updateDate
WHERE yr = $yr
  AND empl_no = $emplNo
  AND appo_doc_yy = $docYear
  AND appo_doc_ch = $docType
  AND appo_doc_seq = $docSeq
  AND resp_status = 0;
""";
        BindKey(update, key);
        update.Parameters.AddWithValue("$updateDate", nowUtc.ToString("O"));
        var affected = await update.ExecuteNonQueryAsync(cancellationToken);

        await using var read = connection.CreateCommand();
        read.Transaction = tx;
        read.CommandText = """
SELECT resp_status, update_date
FROM teach_appo_resp
WHERE yr = $yr
  AND empl_no = $emplNo
  AND appo_doc_yy = $docYear
  AND appo_doc_ch = $docType
  AND appo_doc_seq = $docSeq
LIMIT 1;
""";
        BindKey(read, key);

        await using var reader = await read.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            await tx.CommitAsync(cancellationToken);
            return new AppointmentCompletionResult(false, false, false, null);
        }

        var isCompleted = reader.GetInt32(0) == 1;
        var updatedAtUtc = ParseDateTime(reader.GetString(1));

        await tx.CommitAsync(cancellationToken);
        return new AppointmentCompletionResult(true, isCompleted, affected > 0, updatedAtUtc);
    }

    public async Task<AppointmentAdminRecord> UpsertForAdminAsync(
        AppointmentAdminUpsertInput input,
        DateTime nowUtc,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
INSERT INTO teach_appo_resp (
    yr, empl_no, appo_doc_yy, appo_doc_ch, appo_doc_seq,
    file_name, pdf_content, resp_status, download_count, remark, create_date, update_date)
VALUES (
    $yr, $emplNo, $docYear, $docType, $docSeq,
    $fileName, $pdfContent, $respStatus, $downloadCount, $remark, $createDate, $updateDate)
ON CONFLICT(yr, empl_no, appo_doc_yy, appo_doc_ch, appo_doc_seq) DO UPDATE SET
    file_name = COALESCE(excluded.file_name, teach_appo_resp.file_name),
    pdf_content = COALESCE(excluded.pdf_content, teach_appo_resp.pdf_content),
    resp_status = excluded.resp_status,
    download_count = excluded.download_count,
    remark = excluded.remark,
    update_date = excluded.update_date;
""";
        BindKey(command, input.Key);
        command.Parameters.AddWithValue("$fileName", (object?)input.FileName ?? DBNull.Value);
        command.Parameters.AddWithValue("$pdfContent", (object?)input.PdfContent ?? DBNull.Value);
        command.Parameters.AddWithValue("$respStatus", input.ResponseStatus);
        command.Parameters.AddWithValue("$downloadCount", input.DownloadCount);
        command.Parameters.AddWithValue("$remark", (object?)input.Remark ?? DBNull.Value);
        command.Parameters.AddWithValue("$createDate", nowUtc.ToString("O"));
        command.Parameters.AddWithValue("$updateDate", nowUtc.ToString("O"));
        await command.ExecuteNonQueryAsync(cancellationToken);

        return await GetAdminRecordAsync(input.Key, cancellationToken)
               ?? new AppointmentAdminRecord(input.Key, input.FileName ?? "appointment.pdf", input.ResponseStatus, input.DownloadCount, input.Remark, nowUtc, nowUtc);
    }

    public async Task<AppointmentAdminRecord?> UpdateRemarkForAdminAsync(
        AppointmentDocumentKey key,
        string? remark,
        DateTime nowUtc,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
UPDATE teach_appo_resp
SET remark = $remark,
    update_date = $updateDate
WHERE yr = $yr
  AND empl_no = $emplNo
  AND appo_doc_yy = $docYear
  AND appo_doc_ch = $docType
  AND appo_doc_seq = $docSeq;
""";
        BindKey(command, key);
        command.Parameters.AddWithValue("$remark", (object?)remark ?? DBNull.Value);
        command.Parameters.AddWithValue("$updateDate", nowUtc.ToString("O"));
        var affected = await command.ExecuteNonQueryAsync(cancellationToken);
        if (affected == 0)
        {
            return null;
        }

        return await GetAdminRecordAsync(key, cancellationToken);
    }

    private static async Task<IReadOnlyList<AppointmentResponseSummary>> ReadSummariesAsync(SqliteCommand command, CancellationToken cancellationToken)
    {
        var rows = new List<AppointmentResponseSummary>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            var year = reader.GetInt32(0);
            var employeeNo = reader.GetString(1);
            var docYear = reader.GetInt32(2);
            var docType = reader.GetString(3);
            var docSeq = reader.GetString(4);
            var fileName = reader.IsDBNull(5) ? "appointment" : reader.GetString(5);
            var responded = reader.GetInt32(6) == 1;
            var downloadCount = reader.GetInt32(7);
            var remark = reader.IsDBNull(8) ? null : reader.GetString(8);
            var updatedAtUtc = ParseDateTime(reader.GetString(9));
            var documentNo = $"{docYear}-{docType}-{docSeq}:{fileName}";

            rows.Add(new AppointmentResponseSummary(
                year,
                employeeNo,
                docYear,
                docType,
                docSeq,
                documentNo,
                responded,
                downloadCount,
                remark,
                updatedAtUtc));
        }

        return rows;
    }

    private static DateTime ParseDateTime(string value)
    {
        return DateTime.Parse(value, null, System.Globalization.DateTimeStyles.RoundtripKind);
    }

    private static void EnsureNoPdfContentInSummaryQuery(string sql)
    {
        if (sql.Contains("pdf_content", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Summary/list query cannot select pdf_content.");
        }
    }

    private async Task<AppointmentAdminRecord?> GetAdminRecordAsync(AppointmentDocumentKey key, CancellationToken cancellationToken)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
SELECT file_name, resp_status, download_count, remark, create_date, update_date
FROM teach_appo_resp
WHERE yr = $yr
  AND empl_no = $emplNo
  AND appo_doc_yy = $docYear
  AND appo_doc_ch = $docType
  AND appo_doc_seq = $docSeq
LIMIT 1;
""";
        BindKey(command, key);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        var fileName = reader.IsDBNull(0) ? "appointment.pdf" : reader.GetString(0);
        var responseStatus = reader.GetInt32(1);
        var downloadCount = reader.GetInt32(2);
        var remark = reader.IsDBNull(3) ? null : reader.GetString(3);
        var createdAtUtc = ParseDateTime(reader.GetString(4));
        var updatedAtUtc = ParseDateTime(reader.GetString(5));

        return new AppointmentAdminRecord(key, fileName, responseStatus, downloadCount, remark, createdAtUtc, updatedAtUtc);
    }

    private static void BindKey(SqliteCommand command, AppointmentDocumentKey key)
    {
        command.Parameters.AddWithValue("$yr", key.Year);
        command.Parameters.AddWithValue("$emplNo", key.EmployeeNo);
        command.Parameters.AddWithValue("$docYear", key.DocumentYear);
        command.Parameters.AddWithValue("$docType", key.DocumentType);
        command.Parameters.AddWithValue("$docSeq", key.DocumentSequence);
    }
}
