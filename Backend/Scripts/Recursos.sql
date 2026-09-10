CREATE OR ALTER PROCEDURE dbo.usp_Skill_Consultar AS
BEGIN SELECT ID, Nombre, Categoria, Activo FROM dbo.Skill ORDER BY Activo DESC, Nombre; END
GO
CREATE OR ALTER PROCEDURE dbo.usp_Skill_Registrar @Nombre NVARCHAR(150), @Categoria NVARCHAR(150) = NULL AS
BEGIN INSERT INTO dbo.Skill (Nombre, Categoria, Activo) VALUES (@Nombre, @Categoria, 1); SELECT ID, Nombre, Categoria, Activo FROM dbo.Skill WHERE ID = SCOPE_IDENTITY(); END
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
CREATE OR ALTER PROCEDURE dbo.usp_DisponibilidadBase_RegistrarActualizar @IdEmpleado INT, @HoraInicio TIME, @HoraFin TIME, @HorasSemanales DECIMAL(10,2), @Observacion NVARCHAR(500) = NULL AS
BEGIN
    IF EXISTS(SELECT 1 FROM dbo.DisponibilidadBase WHERE IdEmpleado=@IdEmpleado) UPDATE dbo.DisponibilidadBase SET HoraInicio=@HoraInicio, HoraFin=@HoraFin, HorasSemanales=@HorasSemanales, Observacion=@Observacion, Activo=1 WHERE IdEmpleado=@IdEmpleado;
    ELSE INSERT INTO dbo.DisponibilidadBase(IdEmpleado, HoraInicio, HoraFin, HorasSemanales, Observacion, Activo) VALUES(@IdEmpleado, @HoraInicio, @HoraFin, @HorasSemanales, @Observacion, 1);
END
GO
