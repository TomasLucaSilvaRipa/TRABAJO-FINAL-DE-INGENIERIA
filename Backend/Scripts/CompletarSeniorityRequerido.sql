SET XACT_ABORT ON;
BEGIN TRANSACTION;

-- Completa únicamente tareas históricas sin seniority requerido.
-- Conserva cualquier valor que haya sido cargado previamente.
UPDATE dbo.Tarea
SET SeniorityRequerido = CASE
    WHEN LOWER(LTRIM(RTRIM(ISNULL(Complejidad, N'')))) = N'baja' THEN N'Junior'
    WHEN LOWER(LTRIM(RTRIM(ISNULL(Complejidad, N'')))) = N'alta' THEN N'Senior'
    ELSE N'Semisenior'
END
WHERE NULLIF(LTRIM(RTRIM(SeniorityRequerido)), N'') IS NULL;

-- Las plantillas también deben aportar el dato al crear nuevas tareas.
UPDATE dbo.PlantillaTarea
SET SeniorityRecomendado = CASE
    WHEN LOWER(LTRIM(RTRIM(ISNULL(Complejidad, N'')))) = N'baja' THEN N'Junior'
    WHEN LOWER(LTRIM(RTRIM(ISNULL(Complejidad, N'')))) = N'alta' THEN N'Senior'
    ELSE N'Semisenior'
END
WHERE NULLIF(LTRIM(RTRIM(SeniorityRecomendado)), N'') IS NULL;

COMMIT TRANSACTION;
