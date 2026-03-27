//using System;
//using System.Collections.Generic;
//using Microsoft.EntityFrameworkCore;

//namespace LifeLink_V2.Models;

//public partial class AppDbContext : DbContext
//{
//    public AppDbContext()
//    {
//    }

//    public AppDbContext(DbContextOptions<AppDbContext> options)
//        : base(options)
//    {
//    }

//    public virtual DbSet<ActivityLog> ActivityLogs { get; set; }

//    public virtual DbSet<Appointment> Appointments { get; set; }

//    public virtual DbSet<AppointmentMedFile> AppointmentMedFiles { get; set; }

//    public virtual DbSet<AppointmentStatus> AppointmentStatuses { get; set; }

//    public virtual DbSet<City> Cities { get; set; }

//    public virtual DbSet<Governorate> Governorates { get; set; }

//    public virtual DbSet<InsuranceClaim> InsuranceClaims { get; set; }

//    public virtual DbSet<InsuranceCompany> InsuranceCompanies { get; set; }

//    public virtual DbSet<LabTest> LabTests { get; set; }

//    public virtual DbSet<LabTestCategory> LabTestCategories { get; set; }

//    public virtual DbSet<LabTestOrder> LabTestOrders { get; set; }

//    public virtual DbSet<LabTestResult> LabTestResults { get; set; }

//    public virtual DbSet<MedFile> MedFiles { get; set; }

//    public virtual DbSet<MedicalRecord> MedicalRecords { get; set; }

//    public virtual DbSet<MedicalRecordMedFile> MedicalRecordMedFiles { get; set; }

//    public virtual DbSet<MedicalSpecialty> MedicalSpecialties { get; set; }

//    public virtual DbSet<Medicine> Medicines { get; set; }

//    public virtual DbSet<Notification> Notifications { get; set; }

//    public virtual DbSet<Patient> Patients { get; set; }

//    public virtual DbSet<Payment> Payments { get; set; }

//    public virtual DbSet<PaymentMethod> PaymentMethods { get; set; }

//    public virtual DbSet<PaymentReceipt> PaymentReceipts { get; set; }

//    public virtual DbSet<Permission> Permissions { get; set; }

//    public virtual DbSet<PharmacyOrder> PharmacyOrders { get; set; }

//    public virtual DbSet<PharmacyOrderItem> PharmacyOrderItems { get; set; }

//    public virtual DbSet<Provider> Providers { get; set; }

//    public virtual DbSet<ProviderDoctor> ProviderDoctors { get; set; }

//    public virtual DbSet<ProviderType> ProviderTypes { get; set; }

//    public virtual DbSet<Role> Roles { get; set; }

//    public virtual DbSet<RolePermission> RolePermissions { get; set; }

//    public virtual DbSet<SystemSetting> SystemSettings { get; set; }

//    public virtual DbSet<User> Users { get; set; }

//    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
//#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
//        => optionsBuilder.UseSqlServer("Server=life2link.com;Database=life2link;User Id=yaseen;Password=test123;TrustServerCertificate=True;Persist Security Info=True;");

//    protected override void OnModelCreating(ModelBuilder modelBuilder)
//    {
//        modelBuilder.UseCollation("Arabic_CI_AS");

//        modelBuilder.Entity<ActivityLog>(entity =>
//        {
//            entity.HasKey(e => e.ActivityLogId).HasName("PK__Activity__19A9B7AF803A15ED");

//            entity.HasIndex(e => e.UserId, "IX_ActivityLogs_User");

//            entity.Property(e => e.Action).HasMaxLength(200);
//            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");
//            entity.Property(e => e.Entity).HasMaxLength(100);
//            entity.Property(e => e.EntityId).HasMaxLength(100);

//            entity.HasOne(d => d.User).WithMany(p => p.ActivityLogs)
//                .HasForeignKey(d => d.UserId)
//                .HasConstraintName("FK_ActivityLogs_User");
//        });

//        modelBuilder.Entity<Appointment>(entity =>
//        {
//            entity.HasKey(e => e.AppointmentId).HasName("PK__Appointm__8ECDFCC2459515A4");

//            entity.HasIndex(e => e.PatientId, "IX_Appointments_Patient");

//            entity.HasIndex(e => new { e.ProviderId, e.ScheduledAt }, "IX_Appointments_Provider_Date");

//            entity.HasIndex(e => e.StatusId, "IX_Appointments_Status");

//            entity.HasIndex(e => e.AppointmentCode, "UQ__Appointm__F67FE26FE9B9B574").IsUnique();

//            entity.Property(e => e.AppointmentCode).HasMaxLength(50);
//            entity.Property(e => e.BookingSource).HasMaxLength(50);
//            entity.Property(e => e.CancelReason).HasMaxLength(500);
//            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");
//            entity.Property(e => e.DurationMinutes).HasDefaultValue(30);
//            entity.Property(e => e.ExchangeRate).HasColumnType("decimal(18, 4)");
//            entity.Property(e => e.PriceSyp)
//                .HasColumnType("decimal(18, 2)")
//                .HasColumnName("PriceSYP");
//            entity.Property(e => e.PriceUsd)
//                .HasColumnType("decimal(18, 2)")
//                .HasColumnName("PriceUSD");
//            entity.Property(e => e.StatusId).HasDefaultValue(1);

//            entity.HasOne(d => d.Doctor).WithMany(p => p.Appointments)
//                .HasForeignKey(d => d.DoctorId)
//                .HasConstraintName("FK_Appointments_Doctor");

//            entity.HasOne(d => d.Patient).WithMany(p => p.Appointments)
//                .HasForeignKey(d => d.PatientId)
//                .OnDelete(DeleteBehavior.ClientSetNull)
//                .HasConstraintName("FK_Appointments_Patient");

//            entity.HasOne(d => d.Provider).WithMany(p => p.Appointments)
//                .HasForeignKey(d => d.ProviderId)
//                .OnDelete(DeleteBehavior.ClientSetNull)
//                .HasConstraintName("FK_Appointments_Provider");

//            entity.HasOne(d => d.Specialty).WithMany(p => p.Appointments)
//                .HasForeignKey(d => d.SpecialtyId)
//                .HasConstraintName("FK_Appointments_Specialty");

//            entity.HasOne(d => d.Status).WithMany(p => p.Appointments)
//                .HasForeignKey(d => d.StatusId)
//                .OnDelete(DeleteBehavior.ClientSetNull)
//                .HasConstraintName("FK_Appointments_Status");
//        });

//        modelBuilder.Entity<AppointmentMedFile>(entity =>
//        {
//            entity.HasKey(e => e.AppointmentMedFileId).HasName("PK__Appointm__1880831199552889");

//            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");

//            entity.HasOne(d => d.Appointment).WithMany(p => p.AppointmentMedFiles)
//                .HasForeignKey(d => d.AppointmentId)
//                .HasConstraintName("FK_AppointmentMedFiles_App");

//            entity.HasOne(d => d.MedFile).WithMany(p => p.AppointmentMedFiles)
//                .HasForeignKey(d => d.MedFileId)
//                .HasConstraintName("FK_AppointmentMedFiles_MedFile");
//        });

//        modelBuilder.Entity<AppointmentStatus>(entity =>
//        {
//            entity.HasKey(e => e.StatusId).HasName("PK__Appointm__C8EE2063C81E2029");

//            entity.HasIndex(e => e.StatusName, "UQ__Appointm__05E7698AA7409429").IsUnique();

//            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");
//            entity.Property(e => e.Description).HasMaxLength(200);
//            entity.Property(e => e.IsActive).HasDefaultValue(true);
//            entity.Property(e => e.StatusName).HasMaxLength(50);
//        });

//        modelBuilder.Entity<City>(entity =>
//        {
//            entity.HasKey(e => e.CityId).HasName("PK__Cities__F2D21B76324F8A62");

//            entity.HasIndex(e => e.GovernorateId, "IX_Cities_Governorate");

//            entity.Property(e => e.CityName).HasMaxLength(100);
//            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");

//            entity.HasOne(d => d.Governorate).WithMany(p => p.Cities)
//                .HasForeignKey(d => d.GovernorateId)
//                .OnDelete(DeleteBehavior.ClientSetNull)
//                .HasConstraintName("FK_Cities_Governorate");
//        });

//        modelBuilder.Entity<Governorate>(entity =>
//        {
//            entity.HasKey(e => e.GovernorateId).HasName("PK__Governor__D314AD9AA2062F0C");

//            entity.HasIndex(e => e.GovernorateName, "UQ__Governor__F213165D66135112").IsUnique();

//            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");
//            entity.Property(e => e.GovernorateName).HasMaxLength(100);
//        });

//        modelBuilder.Entity<InsuranceClaim>(entity =>
//        {
//            entity.HasKey(e => e.ClaimId).HasName("PK__Insuranc__EF2E139B9FA66AA6");

//            entity.HasIndex(e => e.Status, "IX_InsuranceClaims_Status");

//            entity.HasIndex(e => e.ClaimCode, "UQ__Insuranc__17537BFC7B749D92").IsUnique();

//            entity.Property(e => e.ApprovedAmountSyp)
//                .HasColumnType("decimal(18, 2)")
//                .HasColumnName("ApprovedAmountSYP");
//            entity.Property(e => e.ClaimAmountSyp)
//                .HasColumnType("decimal(18, 2)")
//                .HasColumnName("ClaimAmountSYP");
//            entity.Property(e => e.ClaimCode).HasMaxLength(50);
//            entity.Property(e => e.Status)
//                .HasMaxLength(50)
//                .HasDefaultValue("Submitted");
//            entity.Property(e => e.SubmittedAt).HasDefaultValueSql("(sysutcdatetime())");

//            entity.HasOne(d => d.InsuranceCompany).WithMany(p => p.InsuranceClaims)
//                .HasForeignKey(d => d.InsuranceCompanyId)
//                .OnDelete(DeleteBehavior.ClientSetNull)
//                .HasConstraintName("FK_Claims_Insurance");

//            entity.HasOne(d => d.Patient).WithMany(p => p.InsuranceClaims)
//                .HasForeignKey(d => d.PatientId)
//                .OnDelete(DeleteBehavior.ClientSetNull)
//                .HasConstraintName("FK_Claims_Patient");

//            entity.HasOne(d => d.RelatedPayment).WithMany(p => p.InsuranceClaims)
//                .HasForeignKey(d => d.RelatedPaymentId)
//                .HasConstraintName("FK_Claims_Payment");
//        });

//        modelBuilder.Entity<InsuranceCompany>(entity =>
//        {
//            entity.HasKey(e => e.InsuranceCompanyId).HasName("PK__Insuranc__CE9C94A48FB98F2B");

//            entity.HasIndex(e => e.CompanyName, "UQ__Insuranc__9BCE05DCC58AD560").IsUnique();

//            entity.Property(e => e.Active).HasDefaultValue(true);
//            entity.Property(e => e.Address).HasMaxLength(300);
//            entity.Property(e => e.Category).HasMaxLength(100);
//            entity.Property(e => e.CompanyName).HasMaxLength(200);
//            entity.Property(e => e.Country)
//                .HasMaxLength(100)
//                .HasDefaultValue("Syria");
//            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");
//            entity.Property(e => e.Email).HasMaxLength(150);
//            entity.Property(e => e.Phone).HasMaxLength(30);
//            entity.Property(e => e.RegistrationNumber).HasMaxLength(100);
//        });

//        modelBuilder.Entity<LabTest>(entity =>
//        {
//            entity.HasKey(e => e.LabTestId).HasName("PK__LabTests__64D33925DE15937E");

//            entity.HasIndex(e => e.ProviderId, "IX_LabTests_Provider");

//            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");
//            entity.Property(e => e.PriceSyp)
//                .HasColumnType("decimal(18, 2)")
//                .HasColumnName("PriceSYP");
//            entity.Property(e => e.PriceUsd)
//                .HasColumnType("decimal(18, 2)")
//                .HasColumnName("PriceUSD");
//            entity.Property(e => e.TestCode).HasMaxLength(50);
//            entity.Property(e => e.TestName).HasMaxLength(200);

//            entity.HasOne(d => d.Category).WithMany(p => p.LabTests)
//                .HasForeignKey(d => d.CategoryId)
//                .HasConstraintName("FK_LabTests_Category");

//            entity.HasOne(d => d.Provider).WithMany(p => p.LabTests)
//                .HasForeignKey(d => d.ProviderId)
//                .OnDelete(DeleteBehavior.ClientSetNull)
//                .HasConstraintName("FK_LabTests_Provider");
//        });

//        modelBuilder.Entity<LabTestCategory>(entity =>
//        {
//            entity.HasKey(e => e.CategoryId).HasName("PK__LabTestC__19093A0BF25DF01F");

//            entity.HasIndex(e => e.CategoryName, "UQ__LabTestC__8517B2E0BAA5710E").IsUnique();

//            entity.Property(e => e.CategoryName).HasMaxLength(100);
//            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");
//            entity.Property(e => e.Description).HasMaxLength(300);
//        });

//        modelBuilder.Entity<LabTestOrder>(entity =>
//        {
//            entity.HasKey(e => e.LabOrderId).HasName("PK__LabTestO__9CBC017E913EF4CF");

//            entity.HasIndex(e => e.PatientId, "IX_LabOrders_Patient");

//            entity.HasIndex(e => e.OrderCode, "UQ__LabTestO__999B522931EB0200").IsUnique();

//            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");
//            entity.Property(e => e.OrderCode).HasMaxLength(50);
//            entity.Property(e => e.PriceSyp)
//                .HasColumnType("decimal(18, 2)")
//                .HasColumnName("PriceSYP");
//            entity.Property(e => e.PriceUsd)
//                .HasColumnType("decimal(18, 2)")
//                .HasColumnName("PriceUSD");
//            entity.Property(e => e.Status)
//                .HasMaxLength(50)
//                .HasDefaultValue("Pending");

//            entity.HasOne(d => d.Patient).WithMany(p => p.LabTestOrders)
//                .HasForeignKey(d => d.PatientId)
//                .OnDelete(DeleteBehavior.ClientSetNull)
//                .HasConstraintName("FK_LabOrder_Patient");

//            entity.HasOne(d => d.Provider).WithMany(p => p.LabTestOrders)
//                .HasForeignKey(d => d.ProviderId)
//                .OnDelete(DeleteBehavior.ClientSetNull)
//                .HasConstraintName("FK_LabOrder_Lab");

//            entity.HasOne(d => d.Test).WithMany(p => p.LabTestOrders)
//                .HasForeignKey(d => d.TestId)
//                .OnDelete(DeleteBehavior.ClientSetNull)
//                .HasConstraintName("FK_LabOrder_Test");
//        });

//        modelBuilder.Entity<LabTestResult>(entity =>
//        {
//            entity.HasKey(e => e.LabTestResultId).HasName("PK__LabTestR__D5812DC333308013");

//            entity.HasIndex(e => e.LabOrderId, "UQ__LabTestR__9CBC017F9C6A2377").IsUnique();

//            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");

//            entity.HasOne(d => d.FullReportMedFile).WithMany(p => p.LabTestResults)
//                .HasForeignKey(d => d.FullReportMedFileId)
//                .HasConstraintName("FK_LabResult_MedFile");

//            entity.HasOne(d => d.LabOrder).WithOne(p => p.LabTestResult)
//                .HasForeignKey<LabTestResult>(d => d.LabOrderId)
//                .HasConstraintName("FK_LabResult_Order");
//        });

//        modelBuilder.Entity<MedFile>(entity =>
//        {
//            entity.HasKey(e => e.MedFileId).HasName("PK__MedFiles__E5ADECDD54C7F433");

//            entity.Property(e => e.ContentType).HasMaxLength(100);
//            entity.Property(e => e.IsPrivate).HasDefaultValue(true);
//            entity.Property(e => e.MedFileName).HasMaxLength(300);
//            entity.Property(e => e.MedFilePath).HasMaxLength(500);
//            entity.Property(e => e.UploadedAt).HasDefaultValueSql("(sysutcdatetime())");
//        });

//        modelBuilder.Entity<MedicalRecord>(entity =>
//        {
//            entity.HasKey(e => e.MedicalRecordId).HasName("PK__MedicalR__4411BA2270373514");

//            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");
//            entity.Property(e => e.RecordedAt).HasDefaultValueSql("(sysutcdatetime())");
//            entity.Property(e => e.Title).HasMaxLength(250);

//            entity.HasOne(d => d.Patient).WithMany(p => p.MedicalRecords)
//                .HasForeignKey(d => d.PatientId)
//                .HasConstraintName("FK_MedicalRecords_Patient");
//        });

//        modelBuilder.Entity<MedicalRecordMedFile>(entity =>
//        {
//            entity.HasKey(e => e.MedicalRecordMedFileId).HasName("PK__MedicalR__A0E9449D60D80580");

//            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");

//            entity.HasOne(d => d.MedFile).WithMany(p => p.MedicalRecordMedFiles)
//                .HasForeignKey(d => d.MedFileId)
//                .HasConstraintName("FK_MedRecMedFiles_MedFile");

//            entity.HasOne(d => d.MedicalRecord).WithMany(p => p.MedicalRecordMedFiles)
//                .HasForeignKey(d => d.MedicalRecordId)
//                .HasConstraintName("FK_MedRecMedFiles_Record");
//        });

//        modelBuilder.Entity<MedicalSpecialty>(entity =>
//        {
//            entity.HasKey(e => e.SpecialtyId).HasName("PK__MedicalS__D768F6A807E1CC0F");

//            entity.HasIndex(e => e.SpecialtyName, "UQ__MedicalS__7DCA574845F98FAD").IsUnique();

//            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");
//            entity.Property(e => e.Description).HasMaxLength(500);
//            entity.Property(e => e.SpecialtyName).HasMaxLength(150);
//        });

//        modelBuilder.Entity<Medicine>(entity =>
//        {
//            entity.HasKey(e => e.MedicineId).HasName("PK__Medicine__4F21289080554FB4");

//            entity.HasIndex(e => e.ProviderId, "IX_Medicines_Provider");

//            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");
//            entity.Property(e => e.Dosage).HasMaxLength(100);
//            entity.Property(e => e.LowStockThreshold).HasDefaultValue(10);
//            entity.Property(e => e.MedicineName).HasMaxLength(250);
//            entity.Property(e => e.PriceSyp)
//                .HasColumnType("decimal(18, 2)")
//                .HasColumnName("PriceSYP");
//            entity.Property(e => e.PriceUsd)
//                .HasColumnType("decimal(18, 2)")
//                .HasColumnName("PriceUSD");

//            entity.HasOne(d => d.Provider).WithMany(p => p.Medicines)
//                .HasForeignKey(d => d.ProviderId)
//                .OnDelete(DeleteBehavior.ClientSetNull)
//                .HasConstraintName("FK_Medicines_Provider");
//        });

//        modelBuilder.Entity<Notification>(entity =>
//        {
//            entity.HasKey(e => e.NotificationId).HasName("PK__Notifica__20CF2E121AC4A194");

//            entity.HasIndex(e => e.UserId, "IX_Notifications_User");

//            entity.Property(e => e.Channel).HasMaxLength(50);
//            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");
//            entity.Property(e => e.Title).HasMaxLength(250);

//            entity.HasOne(d => d.User).WithMany(p => p.Notifications)
//                .HasForeignKey(d => d.UserId)
//                .HasConstraintName("FK_Notifications_User");
//        });

//        modelBuilder.Entity<Patient>(entity =>
//        {
//            entity.HasKey(e => e.PatientId).HasName("PK__Patients__970EC3661736339F");

//            entity.HasIndex(e => e.InsuranceCompanyId, "IX_Patients_Insurance");

//            entity.HasIndex(e => e.UserId, "IX_Patients_User");

//            entity.HasIndex(e => e.UserId, "UQ__Patients__1788CC4D6DD6EE51").IsUnique();

//            entity.Property(e => e.BloodType).HasMaxLength(5);
//            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");
//            entity.Property(e => e.Dob).HasColumnName("DOB");
//            entity.Property(e => e.EmergencyContact).HasMaxLength(150);
//            entity.Property(e => e.EmergencyPhone).HasMaxLength(30);
//            entity.Property(e => e.Gender).HasMaxLength(10);
//            entity.Property(e => e.InsuranceNumber).HasMaxLength(100);
//            entity.Property(e => e.NationalId).HasMaxLength(100);

//            entity.HasOne(d => d.InsuranceCompany).WithMany(p => p.Patients)
//                .HasForeignKey(d => d.InsuranceCompanyId)
//                .HasConstraintName("FK_Patients_Insurance");

//            entity.HasOne(d => d.User).WithOne(p => p.Patient)
//                .HasForeignKey<Patient>(d => d.UserId)
//                .OnDelete(DeleteBehavior.ClientSetNull)
//                .HasConstraintName("FK_Patients_User");
//        });

//        modelBuilder.Entity<Payment>(entity =>
//        {
//            entity.HasKey(e => e.PaymentId).HasName("PK__Payments__9B556A380ED3DAAA");

//            entity.HasIndex(e => e.PatientId, "IX_Payments_Patient");

//            entity.HasIndex(e => e.PaymentStatus, "IX_Payments_Status");

//            entity.Property(e => e.AmountSyp)
//                .HasColumnType("decimal(18, 2)")
//                .HasColumnName("AmountSYP");
//            entity.Property(e => e.AmountUsd)
//                .HasColumnType("decimal(18, 2)")
//                .HasColumnName("AmountUSD");
//            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");
//            entity.Property(e => e.ExchangeRate).HasColumnType("decimal(18, 4)");
//            entity.Property(e => e.InsuranceCoverageAmountSyp)
//                .HasColumnType("decimal(18, 2)")
//                .HasColumnName("InsuranceCoverageAmountSYP");
//            entity.Property(e => e.PatientPayAmountSyp)
//                .HasColumnType("decimal(18, 2)")
//                .HasColumnName("PatientPayAmountSYP");
//            entity.Property(e => e.PaymentRef).HasMaxLength(100);
//            entity.Property(e => e.PaymentStatus)
//                .HasMaxLength(50)
//                .HasDefaultValue("Pending");

//            entity.HasOne(d => d.Patient).WithMany(p => p.Payments)
//                .HasForeignKey(d => d.PatientId)
//                .OnDelete(DeleteBehavior.ClientSetNull)
//                .HasConstraintName("FK_Payments_Patient");

//            entity.HasOne(d => d.PaymentMethod).WithMany(p => p.Payments)
//                .HasForeignKey(d => d.PaymentMethodId)
//                .OnDelete(DeleteBehavior.ClientSetNull)
//                .HasConstraintName("FK_Payments_Method");

//            entity.HasOne(d => d.Provider).WithMany(p => p.Payments)
//                .HasForeignKey(d => d.ProviderId)
//                .HasConstraintName("FK_Payments_Provider");
//        });

//        modelBuilder.Entity<PaymentMethod>(entity =>
//        {
//            entity.HasKey(e => e.PaymentMethodId).HasName("PK__PaymentM__DC31C1D3825D3DAB");

//            entity.HasIndex(e => e.MethodName, "UQ__PaymentM__218CFB1752F64F2B").IsUnique();

//            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");
//            entity.Property(e => e.Description).HasMaxLength(200);
//            entity.Property(e => e.IsActive).HasDefaultValue(true);
//            entity.Property(e => e.MethodName).HasMaxLength(50);
//        });

//        modelBuilder.Entity<PaymentReceipt>(entity =>
//        {
//            entity.HasKey(e => e.ReceiptId).HasName("PK__PaymentR__CC08C42021D8C14D");

//            entity.HasIndex(e => e.ReceiptNumber, "UQ__PaymentR__C08AFDAB52E57951").IsUnique();

//            entity.Property(e => e.IssuedAt).HasDefaultValueSql("(sysutcdatetime())");
//            entity.Property(e => e.ReceiptNumber).HasMaxLength(50);

//            entity.HasOne(d => d.Payment).WithMany(p => p.PaymentReceipts)
//                .HasForeignKey(d => d.PaymentId)
//                .HasConstraintName("FK_Receipt_Payment");
//        });

//        modelBuilder.Entity<Permission>(entity =>
//        {
//            entity.HasKey(e => e.PermissionId).HasName("PK__Permissi__EFA6FB2F8B6AF845");

//            entity.HasIndex(e => e.PermissionKey, "UQ__Permissi__8884ABD4276AD837").IsUnique();

//            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");
//            entity.Property(e => e.Description).HasMaxLength(250);
//            entity.Property(e => e.PermissionKey).HasMaxLength(100);
//        });

//        modelBuilder.Entity<PharmacyOrder>(entity =>
//        {
//            entity.HasKey(e => e.PharmacyOrderId).HasName("PK__Pharmacy__7ECC662847579C4D");

//            entity.HasIndex(e => e.PatientId, "IX_PharmacyOrders_Patient");

//            entity.HasIndex(e => e.OrderCode, "UQ__Pharmacy__999B5229D0A9E77E").IsUnique();

//            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");
//            entity.Property(e => e.OrderCode).HasMaxLength(50);
//            entity.Property(e => e.Status)
//                .HasMaxLength(50)
//                .HasDefaultValue("Pending");
//            entity.Property(e => e.TotalSyp)
//                .HasColumnType("decimal(18, 2)")
//                .HasColumnName("TotalSYP");
//            entity.Property(e => e.TotalUsd)
//                .HasColumnType("decimal(18, 2)")
//                .HasColumnName("TotalUSD");

//            entity.HasOne(d => d.Patient).WithMany(p => p.PharmacyOrders)
//                .HasForeignKey(d => d.PatientId)
//                .OnDelete(DeleteBehavior.ClientSetNull)
//                .HasConstraintName("FK_PharmacyOrders_Patient");

//            entity.HasOne(d => d.Provider).WithMany(p => p.PharmacyOrders)
//                .HasForeignKey(d => d.ProviderId)
//                .OnDelete(DeleteBehavior.ClientSetNull)
//                .HasConstraintName("FK_PharmacyOrders_Provider");
//        });

//        modelBuilder.Entity<PharmacyOrderItem>(entity =>
//        {
//            entity.HasKey(e => e.PharmacyOrderItemId).HasName("PK__Pharmacy__867E7527986658A6");

//            entity.Property(e => e.LineTotalSyp)
//                .HasColumnType("decimal(18, 2)")
//                .HasColumnName("LineTotalSYP");
//            entity.Property(e => e.UnitPriceSyp)
//                .HasColumnType("decimal(18, 2)")
//                .HasColumnName("UnitPriceSYP");
//            entity.Property(e => e.UnitPriceUsd)
//                .HasColumnType("decimal(18, 2)")
//                .HasColumnName("UnitPriceUSD");

//            entity.HasOne(d => d.Medicine).WithMany(p => p.PharmacyOrderItems)
//                .HasForeignKey(d => d.MedicineId)
//                .OnDelete(DeleteBehavior.ClientSetNull)
//                .HasConstraintName("FK_PharmacyOrderItems_Med");

//            entity.HasOne(d => d.PharmacyOrder).WithMany(p => p.PharmacyOrderItems)
//                .HasForeignKey(d => d.PharmacyOrderId)
//                .HasConstraintName("FK_PharmacyOrderItems_Order");
//        });

//        modelBuilder.Entity<Provider>(entity =>
//        {
//            entity.HasKey(e => e.ProviderId).HasName("PK__Provider__B54C687DCF0E6DF7");

//            entity.HasIndex(e => e.CityId, "IX_Providers_City");

//            entity.HasIndex(e => new { e.ProviderTypeId, e.IsActive }, "IX_Providers_Type_Active");

//            entity.HasIndex(e => e.UserId, "UQ__Provider__1788CC4D35414150").IsUnique();

//            entity.Property(e => e.Address).HasMaxLength(300);
//            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");
//            entity.Property(e => e.Email).HasMaxLength(150);
//            entity.Property(e => e.IsActive).HasDefaultValue(true);
//            entity.Property(e => e.LicenseIssuedBy).HasMaxLength(250);
//            entity.Property(e => e.MedicalLicenseNumber).HasMaxLength(150);
//            entity.Property(e => e.Phone).HasMaxLength(30);
//            entity.Property(e => e.ProviderName).HasMaxLength(250);
//            entity.Property(e => e.Rating).HasColumnType("decimal(4, 2)");
//            entity.Property(e => e.TotalAppointments).HasDefaultValue(0);

//            entity.HasOne(d => d.City).WithMany(p => p.Providers)
//                .HasForeignKey(d => d.CityId)
//                .HasConstraintName("FK_Providers_City");

//            entity.HasOne(d => d.ProviderType).WithMany(p => p.Providers)
//                .HasForeignKey(d => d.ProviderTypeId)
//                .OnDelete(DeleteBehavior.ClientSetNull)
//                .HasConstraintName("FK_Providers_Type");

//            entity.HasOne(d => d.User).WithOne(p => p.Provider)
//                .HasForeignKey<Provider>(d => d.UserId)
//                .OnDelete(DeleteBehavior.ClientSetNull)
//                .HasConstraintName("FK_Providers_User");
//        });

//        modelBuilder.Entity<ProviderDoctor>(entity =>
//        {
//            entity.HasKey(e => e.DoctorId).HasName("PK__Provider__2DC00EBF0BA2B6F5");

//            entity.HasIndex(e => e.ProviderId, "IX_Doctors_Provider");

//            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");
//            entity.Property(e => e.Email).HasMaxLength(150);
//            entity.Property(e => e.FullName).HasMaxLength(150);
//            entity.Property(e => e.IsActive).HasDefaultValue(true);
//            entity.Property(e => e.MedicalLicenseNumber).HasMaxLength(150);
//            entity.Property(e => e.Phone).HasMaxLength(30);
//            entity.Property(e => e.WorkingHours).HasMaxLength(200);

//            entity.HasOne(d => d.Provider).WithMany(p => p.ProviderDoctors)
//                .HasForeignKey(d => d.ProviderId)
//                .HasConstraintName("FK_Doctors_Provider");

//            entity.HasOne(d => d.Specialty).WithMany(p => p.ProviderDoctors)
//                .HasForeignKey(d => d.SpecialtyId)
//                .HasConstraintName("FK_Doctors_Specialty");
//        });

//        modelBuilder.Entity<ProviderType>(entity =>
//        {
//            entity.HasKey(e => e.ProviderTypeId).HasName("PK__Provider__2132677730735AB9");

//            entity.HasIndex(e => e.ProviderTypeName, "UQ__Provider__03F69336426D06A1").IsUnique();

//            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");
//            entity.Property(e => e.Description).HasMaxLength(300);
//            entity.Property(e => e.ProviderTypeName).HasMaxLength(100);
//        });

//        modelBuilder.Entity<Role>(entity =>
//        {
//            entity.HasKey(e => e.RoleId).HasName("PK__Roles__8AFACE1AEF705507");

//            entity.HasIndex(e => e.RoleName, "UQ__Roles__8A2B6160D1C92EFB").IsUnique();

//            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");
//            entity.Property(e => e.Description).HasMaxLength(250);
//            entity.Property(e => e.RoleName).HasMaxLength(50);
//        });

//        modelBuilder.Entity<RolePermission>(entity =>
//        {
//            entity.HasKey(e => e.RolePermissionId).HasName("PK__RolePerm__120F46BA423112FC");

//            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");

//            entity.HasOne(d => d.Permission).WithMany(p => p.RolePermissions)
//                .HasForeignKey(d => d.PermissionId)
//                .HasConstraintName("FK_RolePermissions_Permission");

//            entity.HasOne(d => d.Role).WithMany(p => p.RolePermissions)
//                .HasForeignKey(d => d.RoleId)
//                .HasConstraintName("FK_RolePermissions_Role");
//        });

//        modelBuilder.Entity<SystemSetting>(entity =>
//        {
//            entity.HasKey(e => e.SettingKey).HasName("PK__SystemSe__01E719ACE163B79B");

//            entity.Property(e => e.SettingKey).HasMaxLength(200);
//            entity.Property(e => e.Description).HasMaxLength(500);
//            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("(sysutcdatetime())");
//        });

//        modelBuilder.Entity<User>(entity =>
//        {
//            entity.HasKey(e => e.UserId).HasName("PK__Users__1788CC4CBBF8FAA8");

//            entity.HasIndex(e => e.Email, "IX_Users_Email");

//            entity.HasIndex(e => new { e.RoleId, e.IsActive }, "IX_Users_Role_Active");

//            entity.HasIndex(e => e.Email, "UQ__Users__A9D1053418759F4D").IsUnique();

//            entity.Property(e => e.Address).HasMaxLength(300);
//            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");
//            entity.Property(e => e.Email).HasMaxLength(150);
//            entity.Property(e => e.FullName).HasMaxLength(150);
//            entity.Property(e => e.IsActive).HasDefaultValue(true);
//            entity.Property(e => e.PasswordHash).HasMaxLength(500);
//            entity.Property(e => e.Phone).HasMaxLength(30);

//            entity.HasOne(d => d.City).WithMany(p => p.Users)
//                .HasForeignKey(d => d.CityId)
//                .HasConstraintName("FK_Users_City");

//            entity.HasOne(d => d.Role).WithMany(p => p.Users)
//                .HasForeignKey(d => d.RoleId)
//                .OnDelete(DeleteBehavior.ClientSetNull)
//                .HasConstraintName("FK_Users_Role");
//        });

//        OnModelCreatingPartial(modelBuilder);
//    }

//    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
//}
