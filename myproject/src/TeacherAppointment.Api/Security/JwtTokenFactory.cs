using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using TeacherAppointment.Application.Abstractions.Persistence;
using TeacherAppointment.Application.Abstractions.Security;

namespace TeacherAppointment.Api.Security;

public sealed class JwtTokenFactory : ITokenFactory
{
    private readonly JwtOptions _jwtOptions;

    public JwtTokenFactory(IOptions<JwtOptions> jwtOptions)
    {
        _jwtOptions = jwtOptions.Value;
    }

    public TokenEnvelope CreateAccessToken(TeacherIdentityRecord teacher, DateTime issuedAtUtc, DateTime expiresAtUtc)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, teacher.EmployeeNo),
            new("empl_no", teacher.EmployeeNo),
            new("yr", teacher.Year.ToString()),
            new(ClaimTypes.Role, teacher.Role),
            new("id_no_masked", teacher.IdNoMasked),
            new("name", teacher.Name)
        };

        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtOptions.SigningKey));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var tokenDescriptor = new JwtSecurityToken(
            issuer: _jwtOptions.Issuer,
            audience: _jwtOptions.Audience,
            claims: claims,
            notBefore: issuedAtUtc,
            expires: expiresAtUtc,
            signingCredentials: credentials);

        var tokenValue = new JwtSecurityTokenHandler().WriteToken(tokenDescriptor);
        return new TokenEnvelope(tokenValue, expiresAtUtc);
    }
}
