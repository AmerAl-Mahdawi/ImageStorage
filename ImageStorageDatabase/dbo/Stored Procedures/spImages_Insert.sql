CREATE PROCEDURE [dbo].[spImages_Insert]
    @Id          INT,
    @Name        NVARCHAR (256),
    @Description NVARCHAR (MAX),
    @Type        NVARCHAR (20),
    @Size        INT

AS
BEGIN
	SET NOCOUNT ON;

    INSERT INTO [dbo].Images([Name], [Description], [Type], [Size])
    OUTPUT INSERTED.Id
	VALUES (@Name, @Description, @Type, @Size)
END