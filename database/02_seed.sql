/* =============================================================================
   BCAS Web Portal - seed data
   Usage: sqlcmd -S localhost,1433 -U sa -P <password> -i 02_seed.sql

   Sign-in accounts created by this script (development only - change the
   passwords before the portal is exposed to anyone):
       admin@bcas.edu.ph  / Admin@12345    (Administrator)
       editor@bcas.edu.ph / Editor@12345   (Editor)
   The stored values are BCrypt hashes with a work factor of 12.
   ============================================================================= */

USE BCAS_Web;
GO

SET NOCOUNT ON;
GO

/* ---------------------------------------------------------------- Roles ---- */
MERGE dbo.Roles AS target
USING (VALUES
    (N'Administrator', N'Full access to content, users and settings.'),
    (N'Editor',        N'Can create and update content but cannot delete it.')
) AS source (Name, Description)
ON target.Name = source.Name
WHEN NOT MATCHED BY TARGET THEN
    INSERT (Name, Description) VALUES (source.Name, source.Description);
GO

/* ---------------------------------------------------------------- Users ---- */
MERGE dbo.Users AS target
USING (
    SELECT  v.Email, v.PasswordHash, v.FullName, r.Id AS RoleId
    FROM    (VALUES
                (N'admin@bcas.edu.ph',  N'$2a$12$SgW4ys44cygojQlQCi5mh.UyDqB2/y2PEWJ0TPQatGfI3y1C7XYwe', N'Portal Administrator', N'Administrator'),
                (N'editor@bcas.edu.ph', N'$2a$12$am9IuY5XzuL.Xcs4BkAhTurRWK8l3YlWGADHVWRvIspdJmff3Q7mm', N'Content Editor',       N'Editor')
            ) AS v (Email, PasswordHash, FullName, RoleName)
    JOIN    dbo.Roles r ON r.Name = v.RoleName
) AS source
ON target.Email = source.Email
WHEN NOT MATCHED BY TARGET THEN
    INSERT (Email, PasswordHash, FullName, RoleId, IsActive)
    VALUES (source.Email, source.PasswordHash, source.FullName, source.RoleId, 1);
GO

DECLARE @AdminId INT = (SELECT Id FROM dbo.Users WHERE Email = N'admin@bcas.edu.ph');

/* ------------------------------------------------------------- Programs ---- */
MERGE dbo.Programs AS target
USING (VALUES
    (N'BSIT', N'Bachelor of Science in Information Technology', N'bachelor-of-science-in-information-technology',
     N'A four year program covering software development, networking, database systems and IT service management. Graduates are prepared for careers as developers, systems administrators and IT consultants.',
     N'Baccalaureate', 4, 1),
    (N'BSBA', N'Bachelor of Science in Business Administration', N'bachelor-of-science-in-business-administration',
     N'Builds the management, marketing and financial foundations needed to lead an organisation, with electives in entrepreneurship and human resource management.',
     N'Baccalaureate', 4, 2),
    (N'BEED', N'Bachelor of Elementary Education', N'bachelor-of-elementary-education',
     N'Prepares future grade school teachers through coursework in child development, curriculum design and a full semester of practice teaching.',
     N'Baccalaureate', 4, 3),
    (N'BSHM', N'Bachelor of Science in Hospitality Management', N'bachelor-of-science-in-hospitality-management',
     N'Combines classroom theory with laboratory and internship work in hotel, restaurant and tourism operations.',
     N'Baccalaureate', 4, 4)
) AS source (Code, Name, Slug, Description, DegreeLevel, DurationYears, DisplayOrder)
ON target.Code = source.Code
WHEN NOT MATCHED BY TARGET THEN
    INSERT (Code, Name, Slug, Description, DegreeLevel, DurationYears, IsActive, DisplayOrder)
    VALUES (source.Code, source.Name, source.Slug, source.Description, source.DegreeLevel,
            source.DurationYears, 1, source.DisplayOrder);

/* --------------------------------------------------------- ContentPages ---- */
MERGE dbo.ContentPages AS target
USING (VALUES
    (N'about', N'About BCAS',
     N'Batangas College of Arts and Sciences is a private higher education institution serving the province of Batangas. The college offers baccalaureate programs in information technology, business, education and hospitality management, and is committed to producing graduates who are competent, principled and ready for professional practice.'),
    (N'admission', N'Admission',
     N'Admission runs from March to June for the first semester. Applicants submit Form 138, a PSA birth certificate, a certificate of good moral character and two 2x2 photographs to the registrar, then take the college entrance examination before enrolling.'),
    (N'contact', N'Contact Us',
     N'Registrar''s Office: registrar@bcas.edu.ph. Telephone: (043) 000-0000. The campus is open from Monday to Friday, 8:00 AM to 5:00 PM.')
) AS source (Slug, Title, Content)
ON target.Slug = source.Slug
WHEN NOT MATCHED BY TARGET THEN
    INSERT (Slug, Title, Content, IsPublished) VALUES (source.Slug, source.Title, source.Content, 1);

/* -------------------------------------------------------- Announcements ---- */
MERGE dbo.Announcements AS target
USING (VALUES
    (N'Enrollment for the First Semester is Now Open', N'enrollment-for-the-first-semester-is-now-open',
     N'Returning and incoming students may now enroll for the first semester at the registrar''s office or through the online portal.',
     N'Enrollment for the first semester is open until the end of June. Returning students should settle any outstanding balance before securing their registration form. Incoming freshmen must first take the college entrance examination, which is administered every Tuesday and Thursday at 9:00 AM.',
     N'Admission'),
    (N'BCAS Celebrates its 30th Foundation Day', N'bcas-celebrates-its-30th-foundation-day',
     N'A week of academic, cultural and sports activities marks three decades of service to the province of Batangas.',
     N'The foundation week opens with a holy mass and parade, followed by inter-department competitions, a research colloquium and the alumni homecoming on the final evening. All classes are suspended for the duration of the celebration.',
     N'Campus Life'),
    (N'Scholarship Applications for Incoming Freshmen', N'scholarship-applications-for-incoming-freshmen',
     N'Academic, athletic and financial assistance scholarships are open to incoming first year students.',
     N'Applicants must submit their Form 138, a certificate of good moral character and proof of family income to the guidance office. Academic scholars must maintain a general weighted average of at least 1.75 to keep the grant.',
     N'Scholarship')
) AS source (Title, Slug, Summary, Content, Category)
ON target.Slug = source.Slug
WHEN NOT MATCHED BY TARGET THEN
    INSERT (Title, Slug, Summary, Content, Category, IsPublished, PublishedAt, AuthorId)
    VALUES (source.Title, source.Slug, source.Summary, source.Content, source.Category, 1, SYSUTCDATETIME(), @AdminId);

/* ----------------------------------------------------------------- Faqs ---- */
MERGE dbo.Faqs AS target
USING (VALUES
    (N'How do I enroll at BCAS?',
     N'Submit your Form 138, PSA birth certificate, certificate of good moral character and two 2x2 photographs to the registrar''s office, take the entrance examination, then proceed to the cashier to settle the down payment.',
     N'enroll,enrollment,enrol,register,registration,admission,requirements,apply', N'Admission'),
    (N'What programs are offered?',
     N'BCAS offers Information Technology (BSIT), Business Administration (BSBA), Elementary Education (BEED) and Hospitality Management (BSHM). Each is a four year baccalaureate program.',
     N'program,programs,course,courses,degree,offered,bsit,bsba,beed,bshm', N'Academics'),
    (N'How much is the tuition fee?',
     N'Tuition depends on the program and the number of units enrolled. The registrar publishes the current rates every term, and an installment plan is available for each semester.',
     N'tuition,fee,fees,payment,cost,price,installment,balance', N'Finance'),
    (N'When does the semester start?',
     N'The first semester begins in August and the second semester in January. Exact dates are posted on the announcements page before each term.',
     N'semester,term,start,schedule,calendar,classes,begin', N'Academics'),
    (N'How do I request a transcript of records?',
     N'File a request at the registrar''s office, present a valid ID and settle the processing fee. Transcripts are normally released within five working days.',
     N'transcript,records,tor,document,credentials,request,diploma', N'Registrar'),
    (N'Are scholarships available?',
     N'Yes. Academic, athletic and financial assistance scholarships are available. Applications are filed at the guidance office before the start of each school year.',
     N'scholarship,scholarships,grant,financial,assistance,discount,free', N'Scholarship'),
    (N'How can I contact the college?',
     N'You may email registrar@bcas.edu.ph or call (043) 000-0000 from Monday to Friday, 8:00 AM to 5:00 PM.',
     N'contact,email,phone,number,address,office,hours,reach', N'General')
) AS source (Question, Answer, Keywords, Category)
ON target.Question = source.Question
WHEN NOT MATCHED BY TARGET THEN
    INSERT (Question, Answer, Keywords, Category, IsActive)
    VALUES (source.Question, source.Answer, source.Keywords, source.Category, 1);
GO

PRINT 'BCAS_Web seed data applied.';
GO
