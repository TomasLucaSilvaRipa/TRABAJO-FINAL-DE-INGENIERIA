SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRANSACTION;

DELETE rolPermiso
FROM dbo.RolPermiso rolPermiso
INNER JOIN dbo.Rol rol ON rol.ID = rolPermiso.IdRol
INNER JOIN dbo.Permiso permiso ON permiso.ID = rolPermiso.IdPermiso
WHERE rol.TipoUsuario = N'Dueno'
  AND permiso.Codigo IN
  (
      N'GestionarTareas',
      N'GestionarPlantillasTareas',
      N'VerMisTareas'
  );

COMMIT TRANSACTION;
