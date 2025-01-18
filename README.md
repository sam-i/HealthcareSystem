# Healthcare System

A comprehensive web-based healthcare management system built with ASP.NET Core that facilitates interactions between patients, doctors, and radiologists.

## Features
### User Management
- Multi-role authentication system (Admin, Doctor, Radiologist, Patient)
- Secure login with JWT and cookie-based authentication
- Role-based access control
### Admin Dashboard
- Complete user management (Create, Read, Update, Delete)
- System-wide statistics monitoring
- List view of doctors and radiologists to patients assignments
- Cost tracking and management
### Doctor Features
- Patient management and monitoring
- Task assignment and tracking
- Medical report generation
- Image analysis and disease classification
- Real-time patient condition updates
### Radiologist Features
- Medical image upload and management
- DICOM, JPEG, PNG support
- Image categorization and cost definition
### Patient Features
- Personal health record viewing
- Assigned doctor and radiologist information
- Medical image access
- Treatment cost tracking
- Profile management

## Technology Stack
- **Backend**: ASP.NET Core 6.0
- **Database**: Microsoft SQL Server
- **ORM**: Entity Framework Core
- **Authentication**: JWT + Cookie Authentication
- **Frontend**: 
  - Bootstrap 5
  - jQuery
  - Select2
  - DataTables


### Prerequisites
- .NET 6.0 SDK or later
- SQL Server 2019 or later
- Visual Studio 2022 or any compatible IDE

### Installation
1. Clone the repository
```bash
git clone [repository-url]
```
2. Update the connection string in `appsettings.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=HealthcareSystem;Trusted_Connection=True;TrustServerCertificate=True;"
  }
}
```
3. Open the solution in Visual Studio and restore NuGet packages
4. Apply database migrations:
```bash
dotnet ef database update
```
5. Run the application:
```bash
dotnet run
```

### Key Features Implementation
#### Medical Image Management
- Secure file upload system
- Support for multiple image formats
- Automatic task creation for image uploads
- Cost tracking per image
#### Task Management
- Status tracking (Pending, In Progress, Completed, Cancelled)
- Cost association
- Automatic cost calculations
- Task-image relationships
#### Security
- Antiforgery token implementation
- Secure password hashing
- Role-based authorization
- JWT token management

## Security
The system implements several security measures:
- Password hashing using BCrypt
- CSRF protection
- Secure file upload validation
- Role-based access control
- Input validation and sanitization
