CREATE OR ALTER PROCEDURE dbo.usp_Skill_Consultar AS
BEGIN
    SELECT S.ID, S.Nombre, COALESCE(A.Nombre, S.Categoria) AS Categoria, S.IdAreaSkill, A.Nombre AS NombreArea, S.Activo
    FROM dbo.Skill S LEFT JOIN dbo.AreaSkill A ON A.ID = S.IdAreaSkill
    ORDER BY S.Activo DESC, COALESCE(A.Nombre, S.Categoria), S.Nombre;
END
GO
CREATE OR ALTER PROCEDURE dbo.usp_Skill_Registrar @Nombre NVARCHAR(150), @IdAreaSkill INT AS
BEGIN
    SET NOCOUNT ON;
    IF NOT EXISTS (SELECT 1 FROM dbo.AreaSkill WHERE ID = @IdAreaSkill AND Activo = 1) THROW 51020, 'El área de la skill no es válida.', 1;
    IF EXISTS (SELECT 1 FROM dbo.Skill WHERE Nombre = @Nombre) THROW 51021, 'Ya existe una skill con ese nombre.', 1;
    DECLARE @Categoria NVARCHAR(150) = (SELECT Nombre FROM dbo.AreaSkill WHERE ID = @IdAreaSkill);
    INSERT INTO dbo.Skill (Nombre, Categoria, IdAreaSkill, Activo) VALUES (@Nombre, @Categoria, @IdAreaSkill, 1);
    SELECT S.ID, S.Nombre, S.Categoria, S.IdAreaSkill, A.Nombre AS NombreArea, S.Activo FROM dbo.Skill S INNER JOIN dbo.AreaSkill A ON A.ID=S.IdAreaSkill WHERE S.ID = SCOPE_IDENTITY();
END
GO
CREATE OR ALTER PROCEDURE dbo.usp_Skill_CambiarEstado @ID INT, @Activo BIT AS
BEGIN UPDATE dbo.Skill SET Activo=@Activo WHERE ID=@ID; END
GO
CREATE OR ALTER PROCEDURE dbo.usp_Empleado_ConsultarIdPorUsuarioAgencia @IdUsuario INT, @IdAgencia INT AS
BEGIN SELECT E.ID FROM dbo.Empleado E INNER JOIN dbo.Usuario U ON U.ID=E.IdUsuario WHERE E.IdUsuario=@IdUsuario AND U.IdAgencia=@IdAgencia; END
GO
CREATE OR ALTER PROCEDURE dbo.usp_EmpleadoSkill_Consultar @IdEmpleado INT AS
BEGIN SELECT ID, IdEmpleado, IdSkill, Nivel, Activo FROM dbo.EmpleadoSkill WHERE IdEmpleado=@IdEmpleado AND Activo=1; END
GO
CREATE OR ALTER PROCEDURE dbo.usp_EmpleadoSkill_Limpiar @IdEmpleado INT AS
BEGIN UPDATE dbo.EmpleadoSkill SET Activo=0 WHERE IdEmpleado=@IdEmpleado; END
GO
CREATE OR ALTER PROCEDURE dbo.usp_EmpleadoSkill_Registrar @IdEmpleado INT, @IdSkill INT, @Nivel NVARCHAR(50) = NULL AS
BEGIN
    IF EXISTS(SELECT 1 FROM dbo.EmpleadoSkill WHERE IdEmpleado=@IdEmpleado AND IdSkill=@IdSkill) UPDATE dbo.EmpleadoSkill SET Nivel=@Nivel, Activo=1 WHERE IdEmpleado=@IdEmpleado AND IdSkill=@IdSkill;
    ELSE INSERT INTO dbo.EmpleadoSkill(IdEmpleado, IdSkill, Nivel, Activo) VALUES(@IdEmpleado, @IdSkill, @Nivel, 1);
END
GO
CREATE OR ALTER PROCEDURE dbo.usp_DisponibilidadBase_Consultar @IdEmpleado INT AS
BEGIN SELECT ID, IdEmpleado, HoraInicio, HoraFin, HorasSemanales, Observacion, Activo FROM dbo.DisponibilidadBase WHERE IdEmpleado=@IdEmpleado AND Activo=1; END
GO
CREATE OR ALTER PROCEDURE dbo.usp_DisponibilidadBase_RegistrarActualizar @IdEmpleado INT, @HoraInicio TIME, @HoraFin TIME, @HorasSemanales DECIMAL(10,2), @Observacion NVARCHAR(MAX) = NULL AS
BEGIN
    IF EXISTS(SELECT 1 FROM dbo.DisponibilidadBase WHERE IdEmpleado=@IdEmpleado) UPDATE dbo.DisponibilidadBase SET HoraInicio=@HoraInicio, HoraFin=@HoraFin, HorasSemanales=@HorasSemanales, Observacion=@Observacion, Activo=1 WHERE IdEmpleado=@IdEmpleado;
    ELSE INSERT INTO dbo.DisponibilidadBase(IdEmpleado, HoraInicio, HoraFin, HorasSemanales, Observacion, Activo) VALUES(@IdEmpleado, @HoraInicio, @HoraFin, @HorasSemanales, @Observacion, 1);
END
GO
