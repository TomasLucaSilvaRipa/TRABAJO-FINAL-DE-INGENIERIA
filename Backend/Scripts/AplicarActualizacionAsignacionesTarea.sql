/*
   Ejecutar desde SQLCMD sobre la base TeamBalance.
   Aplica el modelo de asignación distribuida y actualiza los procesos que
   consultaban únicamente Tarea.IdEmpleadoAsignado.
*/
:r .\AsignacionesTarea.sql
:r .\ProyectosTareas.sql
:r .\RegistroHoras.sql
:r .\ConfiguracionAgencia.sql
:r .\Ausencias.sql
:r .\BestFit.sql
:r .\SimulacionImpacto.sql
:r .\CU02-008_Slack.sql
:r .\SkillsDesiertos.sql
:r .\RiesgoRetraso.sql
