namespace TeamBalance.BE.Entidades;

public class AnalisisOcupacionOperativa
{
    public decimal PorcentajeOcupacionGeneral { get; set; }
    public string EstadoCargaGeneral { get; set; } = string.Empty;
    public decimal HorasDisponiblesSemanales { get; set; }
    public decimal HorasEstimadasActivas { get; set; }
    public int CantidadSubutilizados { get; set; }
    public int CantidadBalanceados { get; set; }
    public int CantidadSobrecargados { get; set; }
    public List<OcupacionEmpleado> Empleados { get; set; } = new List<OcupacionEmpleado>();
    public List<OcupacionProyecto> Proyectos { get; set; } = new List<OcupacionProyecto>();
    public List<OcupacionCliente> Clientes { get; set; } = new List<OcupacionCliente>();
}

public class FiltroOcupacionOperativa
{
    public int? IdCliente { get; set; }
    public int? IdProyecto { get; set; }
    public int? IdEmpleado { get; set; }
    public string? EstadoCarga { get; set; }
    public DateTime? Desde { get; set; }
    public DateTime? Hasta { get; set; }
}

public class OcupacionEmpleado
{
    public OcupacionEmpleado()
    {
    }

    public OcupacionEmpleado(int idEmpleado, string nombre, string seniority, decimal horasDisponibles, decimal horasEstimadasActivas, decimal horasRegistradas, decimal porcentajeOcupacion, string estadoCarga, string causaPrincipal, string accionSugerida, List<string> proyectos)
    {
        IdEmpleado = idEmpleado;
        Nombre = nombre;
        Seniority = seniority;
        HorasDisponibles = horasDisponibles;
        HorasEstimadasActivas = horasEstimadasActivas;
        HorasRegistradas = horasRegistradas;
        PorcentajeOcupacion = porcentajeOcupacion;
        EstadoCarga = estadoCarga;
        CausaPrincipal = causaPrincipal;
        AccionSugerida = accionSugerida;
        Proyectos = proyectos;
    }

    public int IdEmpleado { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Seniority { get; set; } = string.Empty;
    public decimal HorasDisponibles { get; set; }
    public decimal HorasEstimadasActivas { get; set; }
    public decimal HorasRegistradas { get; set; }
    public decimal PorcentajeOcupacion { get; set; }
    public string EstadoCarga { get; set; } = string.Empty;
    public string CausaPrincipal { get; set; } = string.Empty;
    public string AccionSugerida { get; set; } = string.Empty;
    public List<string> Proyectos { get; set; } = new List<string>();
}

public class OcupacionProyecto
{
    public int IdProyecto { get; set; }
    public int IdCliente { get; set; }
    public int IdPMResponsable { get; set; }
    public string NombreProyecto { get; set; } = string.Empty;
    public string NombreCliente { get; set; } = string.Empty;
    public decimal HorasEstimadasActivas { get; set; }
    public decimal PorcentajeSobreCargaAgencia { get; set; }
    public List<OcupacionEmpleadoProyecto> Empleados { get; set; } = new List<OcupacionEmpleadoProyecto>();
}

public class OcupacionCliente
{
    public int IdCliente { get; set; }
    public string NombreCliente { get; set; } = string.Empty;
    public decimal HorasEstimadasActivas { get; set; }
    public decimal PorcentajeSobreCargaAgencia { get; set; }
    public List<OcupacionProyecto> Proyectos { get; set; } = new List<OcupacionProyecto>();
}

public class OcupacionEmpleadoProyecto
{
    public int IdEmpleado { get; set; }
    public string NombreEmpleado { get; set; } = string.Empty;
    public decimal HorasEstimadas { get; set; }
    public decimal PorcentajeSobreCargaProyecto { get; set; }
}

public class DetalleOcupacionEmpleado
{
    public OcupacionEmpleado Empleado { get; set; } = new OcupacionEmpleado();
    public List<DetalleOcupacionTarea> Tareas { get; set; } = new List<DetalleOcupacionTarea>();
}

public class DetalleOcupacionTarea
{
    public int IdTarea { get; set; }
    public int IdProyecto { get; set; }
    public int IdPMResponsable { get; set; }
    public string Titulo { get; set; } = string.Empty;
    public string NombreProyecto { get; set; } = string.Empty;
    public string NombreCliente { get; set; } = string.Empty;
    public decimal HorasEstimadas { get; set; }
    public decimal IncidenciaSobreCargaEmpleado { get; set; }
}

public class SolicitudRevisionOperativa
{
    public int ID { get; set; }
    public int IdAgencia { get; set; }
    public int IdEmpleado { get; set; }
    public int? IdProyecto { get; set; }
    public int? IdTarea { get; set; }
    public int IdPMResponsable { get; set; }
    public int IdUsuarioSolicitante { get; set; }
    public string CausaPrincipal { get; set; } = string.Empty;
    public string AccionSugerida { get; set; } = string.Empty;
    public string Estado { get; set; } = string.Empty;
    public DateTime FechaAlta { get; set; }
    public string NombreEmpleado { get; set; } = string.Empty;
    public string NombreProyecto { get; set; } = string.Empty;
    public string NombrePM { get; set; } = string.Empty;
}

public class SolicitudRevisionOperativaNueva
{
    public int IdEmpleado { get; set; }
    public int? IdProyecto { get; set; }
    public int? IdTarea { get; set; }
}

public class RevisionOperativaPendiente
{
    public RevisionOperativaPendiente(int id, string tipo, int? idCliente, string cliente, string referencia, string nombreProyecto, string causaPrincipal, string accionSugerida, string estado, DateTime fechaAlta)
    {
        ID = id;
        Tipo = tipo;
        IdCliente = idCliente;
        Cliente = cliente;
        Referencia = referencia;
        NombreProyecto = nombreProyecto;
        CausaPrincipal = causaPrincipal;
        AccionSugerida = accionSugerida;
        Estado = estado;
        FechaAlta = fechaAlta;
    }

    public int ID { get; set; }
    public string Tipo { get; set; } = string.Empty;
    public int? IdCliente { get; set; }
    public string Cliente { get; set; } = string.Empty;
    public string Referencia { get; set; } = string.Empty;
    public string NombreProyecto { get; set; } = string.Empty;
    public string CausaPrincipal { get; set; } = string.Empty;
    public string AccionSugerida { get; set; } = string.Empty;
    public string Estado { get; set; } = string.Empty;
    public DateTime FechaAlta { get; set; }
}

public class SolicitudRevisionGestion
{
    public int ID { get; set; }
    public int IdAgencia { get; set; }
    public int IdProyecto { get; set; }
    public int? IdTarea { get; set; }
    public int IdPMResponsable { get; set; }
    public int IdUsuarioSolicitante { get; set; }
    public string Tipo { get; set; } = string.Empty;
    public string CausaPrincipal { get; set; } = string.Empty;
    public string AccionSugerida { get; set; } = string.Empty;
    public string Estado { get; set; } = string.Empty;
    public DateTime FechaAlta { get; set; }
    public string NombreProyecto { get; set; } = string.Empty;
    public string? TituloTarea { get; set; }
}

public class SolicitudRevisionConveniencia
{
    public int ID { get; set; }
    public int IdAgencia { get; set; }
    public int IdCliente { get; set; }
    public int IdPMResponsable { get; set; }
    public int IdUsuarioSolicitante { get; set; }
    public string CausaPrincipal { get; set; } = string.Empty;
    public string AccionSugerida { get; set; } = string.Empty;
    public string Estado { get; set; } = string.Empty;
    public DateTime FechaAlta { get; set; }
    public string NombreCliente { get; set; } = string.Empty;
    public string NombrePM { get; set; } = string.Empty;
}

public class AnalisisDesvioHoras
{
    public decimal HorasEstimadas { get; set; }
    public decimal HorasReales { get; set; }
    public decimal DesvioHoras { get; set; }
    public decimal HorasRestantes { get; set; }
    public decimal HorasExcedidas { get; set; }
    public decimal PorcentajeConsumo { get; set; }
    public List<DesvioHorasProyecto> Proyectos { get; set; } = new List<DesvioHorasProyecto>();
}

public class FiltroDesvioHoras
{
    public int? IdProyecto { get; set; }
    public int? IdTarea { get; set; }
    public int? IdEmpleado { get; set; }
    public string? EstadoTarea { get; set; }
    public DateTime? Desde { get; set; }
    public DateTime? Hasta { get; set; }
    public string? NivelDesvio { get; set; }
}

public class DesvioHorasProyecto
{
    public int IdProyecto { get; set; }
    public string NombreProyecto { get; set; } = string.Empty;
    public string NombreCliente { get; set; } = string.Empty;
    public decimal HorasEstimadas { get; set; }
    public decimal HorasReales { get; set; }
    public decimal DesvioHoras { get; set; }
    public decimal HorasRestantes { get; set; }
    public decimal HorasExcedidas { get; set; }
    public decimal PorcentajeConsumo { get; set; }
    public string Estado { get; set; } = string.Empty;
    public List<DesvioHorasTarea> Tareas { get; set; } = new List<DesvioHorasTarea>();
}

public class DesvioHorasTarea
{
    public int IdTarea { get; set; }
    public int? IdEmpleadoResponsable { get; set; }
    public string Titulo { get; set; } = string.Empty;
    public string Responsable { get; set; } = string.Empty;
    public string EstadoTarea { get; set; } = string.Empty;
    public decimal HorasEstimadas { get; set; }
    public decimal HorasReales { get; set; }
    public decimal DesvioHoras { get; set; }
    public decimal HorasRestantes { get; set; }
    public decimal HorasExcedidas { get; set; }
    public decimal PorcentajeConsumo { get; set; }
    public string Estado { get; set; } = string.Empty;
}

public class DetalleDesvioHorasTarea
{
    public int IdProyecto { get; set; }
    public int IdTarea { get; set; }
    public string NombreProyecto { get; set; } = string.Empty;
    public string Titulo { get; set; } = string.Empty;
    public string Responsable { get; set; } = string.Empty;
    public string EstadoTarea { get; set; } = string.Empty;
    public decimal HorasEstimadas { get; set; }
    public decimal HorasReales { get; set; }
    public decimal DesvioHoras { get; set; }
    public decimal HorasRestantes { get; set; }
    public decimal HorasExcedidas { get; set; }
    public decimal PorcentajeConsumo { get; set; }
    public string EstadoDesvio { get; set; } = string.Empty;
    public List<RegistroHora> RegistrosHoras { get; set; } = new List<RegistroHora>();
}

public class AccionAjusteDesvioHoras
{
    public int IdProyecto { get; set; }
    public int? IdTarea { get; set; }
    public string Observacion { get; set; } = string.Empty;
}

public class ComunicacionDesvioHoras
{
    public int IdProyecto { get; set; }
    public FiltroDesvioHoras Filtro { get; set; } = new FiltroDesvioHoras();
}

public class DestinatarioDesvioHoras
{
    public DestinatarioDesvioHoras()
    {
    }

    public DestinatarioDesvioHoras(int idUsuario, string nombre, string email)
    {
        IdUsuario = idUsuario;
        Nombre = nombre;
        Email = email;
    }

    public int IdUsuario { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}

public class AnalisisConvenienciaOperativa
{
    public decimal CostoPlanificado { get; set; }
    public decimal CostoReal { get; set; }
    public decimal DesvioCosto { get; set; }
    public decimal PresupuestoNoConsumido { get; set; }
    public decimal Sobrecosto { get; set; }
    public decimal HorasRealesSinCosto { get; set; }
    public int CantidadClientesEnRevision { get; set; }
    public List<ConvenienciaOperativaCliente> Clientes { get; set; } = new List<ConvenienciaOperativaCliente>();
    public List<ConvenienciaOperativaProyecto> Proyectos { get; set; } = new List<ConvenienciaOperativaProyecto>();
}

public class FiltroConvenienciaOperativa
{
    public int? IdCliente { get; set; }
    public int? IdProyecto { get; set; }
    public string? EstadoProyecto { get; set; }
    public string? NivelDesvioCosto { get; set; }
    public DateTime? Desde { get; set; }
    public DateTime? Hasta { get; set; }
}

public class ConvenienciaOperativaCliente
{
    public int IdCliente { get; set; }
    public string NombreCliente { get; set; } = string.Empty;
    public decimal HorasEstimadas { get; set; }
    public decimal HorasReales { get; set; }
    public decimal CostoPlanificado { get; set; }
    public decimal CostoReal { get; set; }
    public decimal DesvioCosto { get; set; }
    public decimal PresupuestoNoConsumido { get; set; }
    public decimal Sobrecosto { get; set; }
    public decimal HorasRealesSinCosto { get; set; }
    public bool EnRevisionOperativa { get; set; }
    public string EstadoConveniencia { get; set; } = string.Empty;
    public List<ConvenienciaOperativaProyecto> Proyectos { get; set; } = new List<ConvenienciaOperativaProyecto>();
}

public class ConvenienciaOperativaProyecto
{
    public int IdProyecto { get; set; }
    public int IdCliente { get; set; }
    public string NombreProyecto { get; set; } = string.Empty;
    public string NombreCliente { get; set; } = string.Empty;
    public decimal HorasEstimadas { get; set; }
    public decimal HorasReales { get; set; }
    public decimal CostoPlanificado { get; set; }
    public decimal CostoReal { get; set; }
    public decimal DesvioCosto { get; set; }
    public decimal PresupuestoNoConsumido { get; set; }
    public decimal Sobrecosto { get; set; }
    public decimal HorasRealesSinCosto { get; set; }
    public decimal HorasPlanificadasSinCosto { get; set; }
    public bool CostoRealIncompleto { get; set; }
    public string EstadoConveniencia { get; set; } = string.Empty;
    public string FactorPrincipal { get; set; } = string.Empty;
}

public class DetalleConvenienciaOperativa
{
    public ConvenienciaOperativaProyecto Proyecto { get; set; } = new ConvenienciaOperativaProyecto();
    public List<ComposicionCostoOperativo> ComposicionCosto { get; set; } = new List<ComposicionCostoOperativo>();
    public List<SeguimientoConvenienciaOperativa> Seguimientos { get; set; } = new List<SeguimientoConvenienciaOperativa>();
}

public class ComposicionCostoOperativo
{
    public int IdTarea { get; set; }
    public int IdEmpleado { get; set; }
    public string Tarea { get; set; } = string.Empty;
    public string Empleado { get; set; } = string.Empty;
    public string Seniority { get; set; } = string.Empty;
    public decimal CostoHora { get; set; }
    public decimal HorasPlanificadas { get; set; }
    public decimal HorasReales { get; set; }
    public decimal CostoPlanificado { get; set; }
    public decimal CostoReal { get; set; }
    public bool CostoNoConfigurado { get; set; }
}

public class SeguimientoConvenienciaOperativa
{
    public int ID { get; set; }
    public int IdCliente { get; set; }
    public int? IdProyecto { get; set; }
    public string Observacion { get; set; } = string.Empty;
    public string AccionSugerida { get; set; } = string.Empty;
    public DateTime FechaAlta { get; set; }
    public string Usuario { get; set; } = string.Empty;
}

public class NuevoSeguimientoConvenienciaOperativa
{
    public int IdCliente { get; set; }
    public int? IdProyecto { get; set; }
    public string Observacion { get; set; } = string.Empty;
    public string AccionSugerida { get; set; } = string.Empty;
}

public class AnalisisSobrecalificacionPerfiles
{
    public decimal HorasAfectadas { get; set; }
    public decimal ImpactoOperativoEstimado { get; set; }
    public int CantidadTareasAfectadas { get; set; }
    public List<SobrecalificacionTarea> Tareas { get; set; } = new List<SobrecalificacionTarea>();
}

public class SobrecalificacionTarea
{
    public int IdTarea { get; set; }
    public int IdProyecto { get; set; }
    public int IdEmpleado { get; set; }
    public string Titulo { get; set; } = string.Empty;
    public string NombreProyecto { get; set; } = string.Empty;
    public string NombreCliente { get; set; } = string.Empty;
    public string SeniorityRequerido { get; set; } = string.Empty;
    public string SeniorityAsignado { get; set; } = string.Empty;
    public string EmpleadoAsignado { get; set; } = string.Empty;
    public decimal HorasAfectadas { get; set; }
    public decimal ImpactoOperativoEstimado { get; set; }
    public string Nivel { get; set; } = string.Empty;
}

public class SolicitudRevisionSobrecalificacionNueva
{
    public int IdEmpleado { get; set; }
    public int IdProyecto { get; set; }
    public int IdTarea { get; set; }
    public string Observacion { get; set; } = string.Empty;
}
