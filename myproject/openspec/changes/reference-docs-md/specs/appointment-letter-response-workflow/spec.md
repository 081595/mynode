## ADDED Requirements

### Requirement: Role-Scoped Appointment Retrieval
The system SHALL return appointment response records according to caller role and identity scope.

#### Scenario: Teacher retrieves own records
- **WHEN** an authenticated teacher requests appointment records
- **THEN** the system returns only records mapped to that teacher's `empl_no` in the requested year scope

#### Scenario: Admin retrieves cross-user records
- **WHEN** an authenticated admin requests appointment records
- **THEN** the system allows filtering and retrieval across all teachers according to query constraints

### Requirement: Appointment PDF Delivery Rules
The system SHALL provide PDF download for appointment documents while applying role-specific counter behavior.

#### Scenario: Teacher download increments counter
- **WHEN** a teacher downloads an appointment PDF from their own record
- **THEN** the system increments `download_count` for that appointment response record

#### Scenario: Admin preview does not increment counter
- **WHEN** an admin accesses appointment PDF content for administrative review
- **THEN** the system serves the file without incrementing the teacher download counter

### Requirement: Response Completion State Transition
The system SHALL update appointment response status after successful verification and response completion.

#### Scenario: Response marked complete on successful verification
- **WHEN** a teacher completes required verification and confirms response action
- **THEN** the system sets `resp_status` to completed and persists update timestamps

#### Scenario: Verification not complete
- **WHEN** a teacher attempts to finalize appointment response without completed verification
- **THEN** the system rejects completion and preserves existing response status
