SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO
IF OBJECT_ID(N'dbo.ConfiguracionAlertaAgencia',N'U') IS NULL CREATE TABLE dbo.ConfiguracionAlertaAgencia(IdAgencia INT NOT NULL CONSTRAINT PK_ConfiguracionAlertaAgencia PRIMARY KEY,UmbralConsumoHoras DECIMAL(5,2) NOT NULL CONSTRAINT DF_ConfiguracionAlertaAgencia_Umbral DEFAULT 80,NotificacionInternaActiva BIT NOT NULL DEFAULT 1,EmailActivo BIT NOT NULL DEFAULT 1,SlackActivo BIT NOT NULL DEFAULT 1,CONSTRAINT FK_ConfiguracionAlertaAgencia_Agencia FOREIGN KEY(IdAgencia) REFERENCES dbo.Agencia(ID),CONSTRAINT CK_ConfiguracionAlertaAgencia_Umbral CHECK(UmbralConsumoHoras BETWEEN 1 AND 100));
GO
IF OBJECT_ID(N'dbo.AlertaOperativaEmitida',N'U') IS NULL
BEGIN
 CREATE TABLE dbo.AlertaOperativaEmitida(ID INT IDENTITY(1,1) PRIMARY KEY,IdProyecto INT NOT NULL,Tipo NVARCHAR(40) NOT NULL,FechaEmision DATETIME2 NOT NULL DEFAULT SYSDATETIME(),Consumo DECIMAL(8,2) NOT NULL,Avance DECIMAL(8,2) NOT NULL,Activa BIT NOT NULL DEFAULT 1,CONSTRAINT FK_AlertaOperativaEmitida_Proyecto FOREIGN KEY(IdProyecto) REFERENCES dbo.Proyecto(ID));
END
GO
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.AlertaOperativaEmitida') AND name=N'UX_AlertaOperativaEmitida_Activa') CREATE UNIQUE INDEX UX_AlertaOperativaEmitida_Activa ON dbo.AlertaOperativaEmitida(IdProyecto,Tipo) WHERE Activa=1;
GO
CREATE OR ALTER PROCEDURE dbo.usp_AlertaOperativa_ConsultarConfiguracion @IdAgencia INT AS SELECT @IdAgencia IdAgencia,COALESCE(C.UmbralConsumoHoras,80) UmbralConsumoHoras,COALESCE(C.NotificacionInternaActiva,1) NotificacionInternaActiva,COALESCE(C.EmailActivo,1) EmailActivo,COALESCE(C.SlackActivo,1) SlackActivo FROM (SELECT 1 X) B LEFT JOIN dbo.ConfiguracionAlertaAgencia C ON C.IdAgencia=@IdAgencia;
GO
CREATE OR ALTER PROCEDURE dbo.usp_AlertaOperativa_GuardarConfiguracion @IdAgencia INT,@UmbralConsumoHoras DECIMAL(5,2),@NotificacionInternaActiva BIT,@EmailActivo BIT,@SlackActivo BIT AS
BEGIN
 IF @UmbralConsumoHoras<1 OR @UmbralConsumoHoras>100 THROW 51300,'El umbral debe estar entre 1% y 100%.',1;
 MERGE dbo.ConfiguracionAlertaAgencia T USING(SELECT @IdAgencia IdAgencia) S ON T.IdAgencia=S.IdAgencia WHEN MATCHED THEN UPDATE SET UmbralConsumoHoras=@UmbralConsumoHoras,NotificacionInternaActiva=@NotificacionInternaActiva,EmailActivo=@EmailActivo,SlackActivo=@SlackActivo WHEN NOT MATCHED THEN INSERT(IdAgencia,UmbralConsumoHoras,NotificacionInternaActiva,EmailActivo,SlackActivo) VALUES(@IdAgencia,@UmbralConsumoHoras,@NotificacionInternaActiva,@EmailActivo,@SlackActivo);
 EXEC dbo.usp_AlertaOperativa_ConsultarConfiguracion @IdAgencia;
END
GO
CREATE OR ALTER PROCEDURE dbo.usp_AlertaOperativa_Generar AS
BEGIN
 SET NOCOUNT ON;
 DECLARE @Metricas TABLE(IdProyecto INT,IdAgencia INT,Nombre NVARCHAR(200),IdPMResponsable INT,Consumo DECIMAL(8,2),Avance DECIMAL(8,2),Umbral DECIMAL(5,2),Interna BIT,Email BIT,Slack BIT);
 INSERT INTO @Metricas
 SELECT P.ID,P.IdAgencia,P.Nombre,P.IdPMResponsable,
   CAST(CASE WHEN COALESCE(NULLIF(P.HorasEstimadasTotales,0),TE.HorasEstimadas,0)>0 THEN 100.0*ISNULL(RH.HorasReales,0)/COALESCE(NULLIF(P.HorasEstimadasTotales,0),TE.HorasEstimadas) ELSE 0 END AS DECIMAL(8,2)),
   CAST(CASE WHEN ISNULL(TE.HorasEstimadas,0)>0 THEN TE.AvancePonderado/TE.HorasEstimadas ELSE 0 END AS DECIMAL(8,2)),
   COALESCE(C.UmbralConsumoHoras,80),COALESCE(C.NotificacionInternaActiva,1),COALESCE(C.EmailActivo,1),COALESCE(C.SlackActivo,1)
 FROM dbo.Proyecto P LEFT JOIN dbo.ConfiguracionAlertaAgencia C ON C.IdAgencia=P.IdAgencia
 OUTER APPLY(SELECT SUM(T.HorasEstimadas) HorasEstimadas,SUM(T.HorasEstimadas*ISNULL(T.PorcentajeAvance,0)) AvancePonderado FROM dbo.Tarea T WHERE T.IdProyecto=P.ID AND T.Activo=1) TE
 OUTER APPLY(SELECT SUM(R.CantidadHoras) HorasReales FROM dbo.RegistroHora R INNER JOIN dbo.Tarea T ON T.ID=R.IdTarea WHERE T.IdProyecto=P.ID AND R.Activo=1) RH
 WHERE P.Activo=1;

 UPDATE A SET Activa=0 FROM dbo.AlertaOperativaEmitida A INNER JOIN @Metricas M ON M.IdProyecto=A.IdProyecto WHERE A.Activa=1 AND ((A.Tipo=N'ConsumoUmbral' AND M.Consumo<M.Umbral) OR (A.Tipo=N'Desvio70_40' AND NOT(M.Consumo>=70 AND M.Avance<=40)));
 DECLARE @Nuevas TABLE(ID INT,IdProyecto INT,Tipo NVARCHAR(40),Consumo DECIMAL(8,2),Avance DECIMAL(8,2));
 INSERT dbo.AlertaOperativaEmitida(IdProyecto,Tipo,Consumo,Avance) OUTPUT inserted.ID,inserted.IdProyecto,inserted.Tipo,inserted.Consumo,inserted.Avance INTO @Nuevas
 SELECT M.IdProyecto,V.Tipo,M.Consumo,M.Avance FROM @Metricas M CROSS APPLY(VALUES(N'ConsumoUmbral',CASE WHEN M.Consumo>=M.Umbral THEN 1 ELSE 0 END),(N'Desvio70_40',CASE WHEN M.Consumo>=70 AND M.Avance<=40 THEN 1 ELSE 0 END))V(Tipo,Cumple)
 WHERE V.Cumple=1 AND NOT EXISTS(SELECT 1 FROM dbo.AlertaOperativaEmitida A WHERE A.IdProyecto=M.IdProyecto AND A.Tipo=V.Tipo AND A.Activa=1);

 INSERT dbo.NotificacionUsuario(IdUsuario,Titulo,Mensaje)
 SELECT DISTINCT U.ID,CASE N.Tipo WHEN N'Desvio70_40' THEN N'Desvío crítico de horas' ELSE N'Umbral de horas alcanzado' END,
   CASE N.Tipo WHEN N'Desvio70_40' THEN CONCAT(M.Nombre,N' consumió ',N.Consumo,N'% de sus horas con ',N.Avance,N'% de avance.') ELSE CONCAT(M.Nombre,N' alcanzó ',N.Consumo,N'% de consumo de horas.') END
 FROM @Nuevas N INNER JOIN @Metricas M ON M.IdProyecto=N.IdProyecto INNER JOIN dbo.Usuario U ON U.IdAgencia=M.IdAgencia AND U.Activo=1
 WHERE M.Interna=1 AND (EXISTS(SELECT 1 FROM dbo.UsuarioRol UR INNER JOIN dbo.Rol R ON R.ID=UR.IdRol WHERE UR.IdUsuario=U.ID AND R.Activo=1 AND R.TipoUsuario=N'Dueno') OR (N.Tipo=N'ConsumoUmbral' AND EXISTS(SELECT 1 FROM dbo.PM PM WHERE PM.ID=M.IdPMResponsable AND PM.IdUsuario=U.ID AND PM.Activo=1)));

 SELECT N.ID,N.IdProyecto,N.Tipo,M.Nombre Proyecto,N.Consumo,N.Avance,U.Nombre NombreDestinatario,U.Email,US.SlackUserId,M.Email EmailActivo,M.Slack SlackActivo
 FROM @Nuevas N INNER JOIN @Metricas M ON M.IdProyecto=N.IdProyecto INNER JOIN dbo.Usuario U ON U.IdAgencia=M.IdAgencia AND U.Activo=1
 INNER JOIN dbo.UsuarioRol UR ON UR.IdUsuario=U.ID INNER JOIN dbo.Rol R ON R.ID=UR.IdRol AND R.TipoUsuario=N'Dueno' AND R.Activo=1 LEFT JOIN dbo.UsuarioSlack US ON US.IdUsuario=U.ID;
END
GO
