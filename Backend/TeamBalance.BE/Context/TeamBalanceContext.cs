using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using TeamBalance.BE.Entidades;

namespace TeamBalance.BE.Context;

public partial class TeamBalanceContext : DbContext
{
    public TeamBalanceContext(DbContextOptions<TeamBalanceContext> options)
        : base(options)
    {
    }

    public virtual DbSet<AceptacionTermino> AceptacionTerminos { get; set; }

    public virtual DbSet<AgenciaCliente> AgenciaClientes { get; set; }

    public virtual DbSet<Agencia> Agencia { get; set; }

    public virtual DbSet<AsignacionTarea> AsignacionTareas { get; set; }

    public virtual DbSet<AusenciaEmpleado> AusenciaEmpleados { get; set; }

    public virtual DbSet<Bitacora> Bitacoras { get; set; }

    public virtual DbSet<Cliente> Clientes { get; set; }

    public virtual DbSet<ConsultaSoporte> ConsultaSoportes { get; set; }

    public virtual DbSet<ContratacionServicio> ContratacionServicios { get; set; }

    public virtual DbSet<DisponibilidadBase> DisponibilidadBases { get; set; }

    public virtual DbSet<Dueño> Dueños { get; set; }

    public virtual DbSet<Empleado> Empleados { get; set; }

    public virtual DbSet<EmpleadoSkill> EmpleadoSkills { get; set; }

    public virtual DbSet<EstadoTarea> EstadoTareas { get; set; }

    public virtual DbSet<MensajeConsultaSoporte> MensajeConsultaSoportes { get; set; }

    public virtual DbSet<OperacionPago> OperacionPagos { get; set; }

    public virtual DbSet<PM> PMs { get; set; }

    public virtual DbSet<Permiso> Permisos { get; set; }

    public virtual DbSet<PlanComercial> PlanComercials { get; set; }

    public virtual DbSet<PlantillaTarea> PlantillaTareas { get; set; }

    public virtual DbSet<Proyecto> Proyectos { get; set; }

    public virtual DbSet<RecomendacionBestFit> RecomendacionBestFits { get; set; }

    public virtual DbSet<RegistroHora> RegistroHoras { get; set; }

    public virtual DbSet<Rol> Rols { get; set; }

    public virtual DbSet<RolPermiso> RolPermisos { get; set; }

    public virtual DbSet<SesionUsuario> SesionUsuarios { get; set; }

    public virtual DbSet<SimulacionImpacto> SimulacionImpactos { get; set; }

    public virtual DbSet<Skill> Skills { get; set; }

    public virtual DbSet<Suscripcion> Suscripcions { get; set; }

    public virtual DbSet<Tarea> Tareas { get; set; }

    public virtual DbSet<TerminosCondicione> TerminosCondiciones { get; set; }

    public virtual DbSet<Usuario> Usuarios { get; set; }

    public virtual DbSet<ValidacionCuentum> ValidacionCuenta { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AceptacionTermino>(entity =>
        {
            entity.HasIndex(e => new { e.IdUsuario, e.IdTerminosCondiciones }, "UQ_AceptacionTerminos_Usuario_Terminos").IsUnique();

            entity.Property(e => e.DireccionIP).HasMaxLength(50);
            entity.Property(e => e.FechaAceptacion)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())");

            entity.HasOne(d => d.IdTerminosCondicionesNavigation).WithMany(p => p.AceptacionTerminos)
                .HasForeignKey(d => d.IdTerminosCondiciones)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_AceptacionTerminos_Terminos");

            entity.HasOne(d => d.IdUsuarioNavigation).WithMany(p => p.AceptacionTerminos)
                .HasForeignKey(d => d.IdUsuario)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_AceptacionTerminos_Usuario");
        });

        modelBuilder.Entity<AgenciaCliente>(entity =>
        {
            entity.ToTable("AgenciaCliente");

            entity.HasIndex(e => new { e.IdAgencia, e.IdCliente }, "UQ_AgenciaCliente_Agencia_Cliente").IsUnique();

            entity.Property(e => e.Activo).HasDefaultValue(true);
            entity.Property(e => e.FechaAlta)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.FechaBaja).HasPrecision(0);

            entity.HasOne(d => d.IdAgenciaNavigation).WithMany(p => p.AgenciaClientes)
                .HasForeignKey(d => d.IdAgencia)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_AgenciaCliente_Agencia");

            entity.HasOne(d => d.IdClienteNavigation).WithMany(p => p.AgenciaClientes)
                .HasForeignKey(d => d.IdCliente)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_AgenciaCliente_Cliente");
        });

        modelBuilder.Entity<Agencia>(entity =>
        {
            entity.HasIndex(e => e.CUIT, "UQ_Agencia_CUIT").IsUnique();

            entity.HasIndex(e => e.EmailContacto, "UQ_Agencia_EmailContacto").IsUnique();

            entity.Property(e => e.Activo).HasDefaultValue(true);
            entity.Property(e => e.CUIT).HasMaxLength(20);
            entity.Property(e => e.CondicionFiscal).HasMaxLength(80);
            entity.Property(e => e.EmailContacto).HasMaxLength(150);
            entity.Property(e => e.Estado)
                .HasMaxLength(50)
                .HasDefaultValue("Activa");
            entity.Property(e => e.FechaAlta)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.FechaBaja).HasPrecision(0);
            entity.Property(e => e.NombreComercial).HasMaxLength(150);
            entity.Property(e => e.RazonSocial).HasMaxLength(200);
            entity.Property(e => e.TelefonoContacto).HasMaxLength(50);
        });

        modelBuilder.Entity<AsignacionTarea>(entity =>
        {
            entity.ToTable("AsignacionTarea");

            entity.Property(e => e.Activa).HasDefaultValue(true);
            entity.Property(e => e.FechaDesde)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.FechaHasta).HasPrecision(0);
            entity.Property(e => e.Motivo).HasMaxLength(500);

            entity.HasOne(d => d.IdEmpleadoNavigation).WithMany(p => p.AsignacionTareas)
                .HasForeignKey(d => d.IdEmpleado)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_AsignacionTarea_Empleado");

            entity.HasOne(d => d.IdTareaNavigation).WithMany(p => p.AsignacionTareas)
                .HasForeignKey(d => d.IdTarea)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_AsignacionTarea_Tarea");

            entity.HasOne(d => d.IdUsuarioAsignadorNavigation).WithMany(p => p.AsignacionTareas)
                .HasForeignKey(d => d.IdUsuarioAsignador)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_AsignacionTarea_Usuario");
        });

        modelBuilder.Entity<AusenciaEmpleado>(entity =>
        {
            entity.ToTable("AusenciaEmpleado");

            entity.Property(e => e.Activo).HasDefaultValue(true);
            entity.Property(e => e.Estado)
                .HasMaxLength(50)
                .HasDefaultValue("Pendiente");
            entity.Property(e => e.FechaFinAprobada).HasPrecision(0);
            entity.Property(e => e.FechaFinSolicitada).HasPrecision(0);
            entity.Property(e => e.FechaInicioAprobada).HasPrecision(0);
            entity.Property(e => e.FechaInicioSolicitada).HasPrecision(0);
            entity.Property(e => e.FechaResolucion).HasPrecision(0);
            entity.Property(e => e.FechaSolicitud)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.HorasNoDisponiblesAprobadas).HasColumnType("decimal(5, 2)");
            entity.Property(e => e.HorasNoDisponiblesSolicitadas).HasColumnType("decimal(5, 2)");
            entity.Property(e => e.Motivo).HasMaxLength(500);
            entity.Property(e => e.MotivoResolucion).HasMaxLength(500);
            entity.Property(e => e.TipoPeriodo).HasMaxLength(80);

            entity.HasOne(d => d.IdEmpleadoNavigation).WithMany(p => p.AusenciaEmpleados)
                .HasForeignKey(d => d.IdEmpleado)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_AusenciaEmpleado_Empleado");

            entity.HasOne(d => d.IdUsuarioResolucionNavigation).WithMany(p => p.AusenciaEmpleados)
                .HasForeignKey(d => d.IdUsuarioResolucion)
                .HasConstraintName("FK_AusenciaEmpleado_UsuarioResolucion");
        });

        modelBuilder.Entity<Bitacora>(entity =>
        {
            entity.ToTable("Bitacora");

            entity.HasIndex(e => new { e.Entidad, e.IdEntidad }, "IX_Bitacora_Entidad_IdEntidad");

            entity.HasIndex(e => new { e.IdAgencia, e.FechaHora }, "IX_Bitacora_IdAgencia_FechaHora");

            entity.Property(e => e.Accion).HasMaxLength(100);
            entity.Property(e => e.Criticidad).HasMaxLength(50);
            entity.Property(e => e.DireccionIP).HasMaxLength(50);
            entity.Property(e => e.Entidad).HasMaxLength(100);
            entity.Property(e => e.FechaHora)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.Mensaje).HasMaxLength(1000);
            entity.Property(e => e.Modulo).HasMaxLength(100);
            entity.Property(e => e.Resultado).HasMaxLength(50);

            entity.HasOne(d => d.IdAgenciaNavigation).WithMany(p => p.Bitacoras)
                .HasForeignKey(d => d.IdAgencia)
                .HasConstraintName("FK_Bitacora_Agencia");

            entity.HasOne(d => d.IdUsuarioNavigation).WithMany(p => p.Bitacoras)
                .HasForeignKey(d => d.IdUsuario)
                .HasConstraintName("FK_Bitacora_Usuario");
        });

        modelBuilder.Entity<Cliente>(entity =>
        {
            entity.ToTable("Cliente");

            entity.Property(e => e.Activo).HasDefaultValue(true);
            entity.Property(e => e.Email).HasMaxLength(150);
            entity.Property(e => e.FechaBaja).HasPrecision(0);
            entity.Property(e => e.Nombre).HasMaxLength(150);
            entity.Property(e => e.RazonSocial).HasMaxLength(200);
            entity.Property(e => e.Telefono).HasMaxLength(50);
        });

        modelBuilder.Entity<ConsultaSoporte>(entity =>
        {
            entity.ToTable("ConsultaSoporte");

            entity.Property(e => e.Activo).HasDefaultValue(true);
            entity.Property(e => e.Asunto).HasMaxLength(200);
            entity.Property(e => e.Estado)
                .HasMaxLength(50)
                .HasDefaultValue("Abierta");
            entity.Property(e => e.FechaActualizacion).HasPrecision(0);
            entity.Property(e => e.FechaBaja).HasPrecision(0);
            entity.Property(e => e.FechaCreacion)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())");

            entity.HasOne(d => d.IdAgenciaNavigation).WithMany(p => p.ConsultaSoportes)
                .HasForeignKey(d => d.IdAgencia)
                .HasConstraintName("FK_ConsultaSoporte_Agencia");

            entity.HasOne(d => d.IdUsuarioNavigation).WithMany(p => p.ConsultaSoportes)
                .HasForeignKey(d => d.IdUsuario)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ConsultaSoporte_Usuario");
        });

        modelBuilder.Entity<ContratacionServicio>(entity =>
        {
            entity.ToTable("ContratacionServicio");

            entity.HasIndex(e => e.ReferenciaContratacion, "UQ_ContratacionServicio_Referencia").IsUnique();

            entity.Property(e => e.Activo).HasDefaultValue(true);
            entity.Property(e => e.ApellidoResponsable).HasMaxLength(100);
            entity.Property(e => e.CUIT).HasMaxLength(20);
            entity.Property(e => e.CargoResponsable).HasMaxLength(100);
            entity.Property(e => e.CondicionFiscal).HasMaxLength(80);
            entity.Property(e => e.EmailFacturacion).HasMaxLength(150);
            entity.Property(e => e.EmailLaboralResponsable).HasMaxLength(150);
            entity.Property(e => e.EstadoContratacion)
                .HasMaxLength(50)
                .HasDefaultValue("Pendiente");
            entity.Property(e => e.FechaBaja).HasPrecision(0);
            entity.Property(e => e.FechaRespuesta).HasPrecision(0);
            entity.Property(e => e.FechaSolicitud)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.MensajeRespuesta).HasMaxLength(1000);
            entity.Property(e => e.NombreComercialAgencia).HasMaxLength(150);
            entity.Property(e => e.NombreResponsable).HasMaxLength(100);
            entity.Property(e => e.ProveedorPagoSeleccionado).HasMaxLength(100);
            entity.Property(e => e.RazonSocial).HasMaxLength(200);
            entity.Property(e => e.ReferenciaContratacion).HasMaxLength(250);
            entity.Property(e => e.TelefonoContacto).HasMaxLength(50);

            entity.HasOne(d => d.IdAgenciaNavigation).WithMany(p => p.ContratacionServicios)
                .HasForeignKey(d => d.IdAgencia)
                .HasConstraintName("FK_ContratacionServicio_Agencia");

            entity.HasOne(d => d.IdPlanComercialNavigation).WithMany(p => p.ContratacionServicios)
                .HasForeignKey(d => d.IdPlanComercial)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ContratacionServicio_PlanComercial");

            entity.HasOne(d => d.IdUsuarioNavigation).WithMany(p => p.ContratacionServicios)
                .HasForeignKey(d => d.IdUsuario)
                .HasConstraintName("FK_ContratacionServicio_Usuario");
        });

        modelBuilder.Entity<DisponibilidadBase>(entity =>
        {
            entity.ToTable("DisponibilidadBase");

            entity.HasIndex(e => e.IdEmpleado, "UQ_DisponibilidadBase_Empleado").IsUnique();

            entity.Property(e => e.Activo).HasDefaultValue(true);
            entity.Property(e => e.HoraFin).HasPrecision(0);
            entity.Property(e => e.HoraInicio).HasPrecision(0);
            entity.Property(e => e.HorasSemanales).HasColumnType("decimal(5, 2)");
            entity.Property(e => e.Observacion).HasMaxLength(500);

            entity.HasOne(d => d.IdEmpleadoNavigation).WithOne(p => p.DisponibilidadBase)
                .HasForeignKey<DisponibilidadBase>(d => d.IdEmpleado)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_DisponibilidadBase_Empleado");
        });

        modelBuilder.Entity<Dueño>(entity =>
        {
            entity.ToTable("Dueño");

            entity.HasIndex(e => e.IdUsuario, "UQ_Dueño_IdUsuario").IsUnique();

            entity.Property(e => e.Activo).HasDefaultValue(true);
            entity.Property(e => e.FechaBaja).HasPrecision(0);

            entity.HasOne(d => d.IdUsuarioNavigation).WithOne(p => p.Dueño)
                .HasForeignKey<Dueño>(d => d.IdUsuario)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Dueño_Usuario");
        });

        modelBuilder.Entity<Empleado>(entity =>
        {
            entity.ToTable("Empleado");

            entity.HasIndex(e => e.IdUsuario, "UQ_Empleado_IdUsuario").IsUnique();

            entity.Property(e => e.Activo).HasDefaultValue(true);
            entity.Property(e => e.CostoHora).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.EstadoLaboral)
                .HasMaxLength(50)
                .HasDefaultValue("Activo");
            entity.Property(e => e.FechaBaja).HasPrecision(0);
            entity.Property(e => e.FechaIngreso).HasPrecision(0);
            entity.Property(e => e.HorasDisponiblesSemanales).HasColumnType("decimal(5, 2)");
            entity.Property(e => e.Seniority).HasMaxLength(50);

            entity.HasOne(d => d.IdUsuarioNavigation).WithOne(p => p.Empleado)
                .HasForeignKey<Empleado>(d => d.IdUsuario)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Empleado_Usuario");
        });

        modelBuilder.Entity<EmpleadoSkill>(entity =>
        {
            entity.ToTable("EmpleadoSkill");

            entity.HasIndex(e => new { e.IdEmpleado, e.IdSkill }, "UQ_EmpleadoSkill_Empleado_Skill").IsUnique();

            entity.Property(e => e.Activo).HasDefaultValue(true);
            entity.Property(e => e.Nivel).HasMaxLength(50);

            entity.HasOne(d => d.IdEmpleadoNavigation).WithMany(p => p.EmpleadoSkills)
                .HasForeignKey(d => d.IdEmpleado)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_EmpleadoSkill_Empleado");

            entity.HasOne(d => d.IdSkillNavigation).WithMany(p => p.EmpleadoSkills)
                .HasForeignKey(d => d.IdSkill)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_EmpleadoSkill_Skill");
        });

        modelBuilder.Entity<EstadoTarea>(entity =>
        {
            entity.ToTable("EstadoTarea");

            entity.HasIndex(e => new { e.IdAgencia, e.Nombre }, "UQ_EstadoTarea_Agencia_Nombre").IsUnique();

            entity.Property(e => e.Activo).HasDefaultValue(true);
            entity.Property(e => e.Nombre).HasMaxLength(80);

            entity.HasOne(d => d.IdAgenciaNavigation).WithMany(p => p.EstadoTareas)
                .HasForeignKey(d => d.IdAgencia)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_EstadoTarea_Agencia");
        });

        modelBuilder.Entity<MensajeConsultaSoporte>(entity =>
        {
            entity.ToTable("MensajeConsultaSoporte");

            entity.Property(e => e.Activo).HasDefaultValue(true);
            entity.Property(e => e.Fecha)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())");

            entity.HasOne(d => d.IdConsultaSoporteNavigation).WithMany(p => p.MensajeConsultaSoportes)
                .HasForeignKey(d => d.IdConsultaSoporte)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_MensajeConsultaSoporte_Consulta");

            entity.HasOne(d => d.IdUsuarioNavigation).WithMany(p => p.MensajeConsultaSoportes)
                .HasForeignKey(d => d.IdUsuario)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_MensajeConsultaSoporte_Usuario");
        });

        modelBuilder.Entity<OperacionPago>(entity =>
        {
            entity.ToTable("OperacionPago");

            entity.HasIndex(e => e.ReferenciaInterna, "UQ_OperacionPago_ReferenciaInterna").IsUnique();

            entity.Property(e => e.Estado).HasMaxLength(50);
            entity.Property(e => e.FechaActualizacion).HasPrecision(0);
            entity.Property(e => e.FechaAprobacion).HasPrecision(0);
            entity.Property(e => e.FechaCreacion)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.Importe).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.Moneda)
                .HasMaxLength(10)
                .HasDefaultValue("ARS");
            entity.Property(e => e.Proveedor).HasMaxLength(100);
            entity.Property(e => e.ReferenciaInterna).HasMaxLength(250);
            entity.Property(e => e.ReferenciaProveedor).HasMaxLength(250);

            entity.HasOne(d => d.IdContratacionServicioNavigation).WithMany(p => p.OperacionPagos)
                .HasForeignKey(d => d.IdContratacionServicio)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_OperacionPago_ContratacionServicio");
        });

        modelBuilder.Entity<PM>(entity =>
        {
            entity.ToTable("PM");

            entity.HasIndex(e => e.IdUsuario, "UQ_PM_IdUsuario").IsUnique();

            entity.Property(e => e.Activo).HasDefaultValue(true);
            entity.Property(e => e.AutorizadoGestionRecursos).HasDefaultValue(true);
            entity.Property(e => e.FechaBaja).HasPrecision(0);

            entity.HasOne(d => d.IdUsuarioNavigation).WithOne(p => p.PM)
                .HasForeignKey<PM>(d => d.IdUsuario)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_PM_Usuario");
        });

        modelBuilder.Entity<Permiso>(entity =>
        {
            entity.ToTable("Permiso");

            entity.HasIndex(e => e.Nombre, "UQ_Permiso_Nombre").IsUnique();

            entity.Property(e => e.Activo).HasDefaultValue(true);
            entity.Property(e => e.Descripcion).HasMaxLength(300);
            entity.Property(e => e.Nombre).HasMaxLength(120);
        });

        modelBuilder.Entity<PlanComercial>(entity =>
        {
            entity.ToTable("PlanComercial");

            entity.HasIndex(e => new { e.Nombre, e.Periodicidad }, "UQ_PlanComercial_Nombre_Periodicidad").IsUnique();

            entity.Property(e => e.Activo).HasDefaultValue(true);
            entity.Property(e => e.AlcanceFuncional).HasMaxLength(1000);
            entity.Property(e => e.CondicionesRenovacion).HasMaxLength(1000);
            entity.Property(e => e.Descripcion).HasMaxLength(500);
            entity.Property(e => e.FechaVigenciaDesde)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.FechaVigenciaHasta).HasPrecision(0);
            entity.Property(e => e.Moneda)
                .HasMaxLength(10)
                .HasDefaultValue("ARS");
            entity.Property(e => e.Nombre).HasMaxLength(100);
            entity.Property(e => e.Periodicidad).HasMaxLength(50);
            entity.Property(e => e.PrecioVigente).HasColumnType("decimal(18, 2)");
        });

        modelBuilder.Entity<PlantillaTarea>(entity =>
        {
            entity.ToTable("PlantillaTarea");

            entity.Property(e => e.Activo).HasDefaultValue(true);
            entity.Property(e => e.Complejidad).HasMaxLength(50);
            entity.Property(e => e.DescripcionBase).HasMaxLength(1000);
            entity.Property(e => e.Estado)
                .HasMaxLength(50)
                .HasDefaultValue("Activa");
            entity.Property(e => e.FechaBaja).HasPrecision(0);
            entity.Property(e => e.FechaCreacion)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.HorasEstimadas).HasColumnType("decimal(10, 2)");
            entity.Property(e => e.Nombre).HasMaxLength(150);
            entity.Property(e => e.PrioridadSugerida).HasMaxLength(50);
            entity.Property(e => e.SeniorityRecomendado).HasMaxLength(50);
            entity.Property(e => e.SkillRequerido).HasMaxLength(100);
            entity.Property(e => e.TituloSugerido).HasMaxLength(150);

            entity.HasOne(d => d.IdAgenciaNavigation).WithMany(p => p.PlantillaTareas)
                .HasForeignKey(d => d.IdAgencia)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_PlantillaTarea_Agencia");

            entity.HasOne(d => d.IdSkillRequeridoNavigation).WithMany(p => p.PlantillaTareas)
                .HasForeignKey(d => d.IdSkillRequerido)
                .HasConstraintName("FK_PlantillaTarea_Skill");
        });

        modelBuilder.Entity<Proyecto>(entity =>
        {
            entity.ToTable("Proyecto");

            entity.HasIndex(e => e.IdAgencia, "IX_Proyecto_IdAgencia");

            entity.HasIndex(e => e.IdCliente, "IX_Proyecto_IdCliente");

            entity.Property(e => e.Activo).HasDefaultValue(true);
            entity.Property(e => e.Deadline).HasPrecision(0);
            entity.Property(e => e.Descripcion).HasMaxLength(1000);
            entity.Property(e => e.Estado)
                .HasMaxLength(50)
                .HasDefaultValue("Activo");
            entity.Property(e => e.FechaAlta)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.FechaBaja).HasPrecision(0);
            entity.Property(e => e.FechaInicio).HasPrecision(0);
            entity.Property(e => e.HorasEstimadasTotales).HasColumnType("decimal(10, 2)");
            entity.Property(e => e.Nombre).HasMaxLength(150);

            entity.HasOne(d => d.IdAgenciaNavigation).WithMany(p => p.Proyectos)
                .HasForeignKey(d => d.IdAgencia)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Proyecto_Agencia");

            entity.HasOne(d => d.IdClienteNavigation).WithMany(p => p.Proyectos)
                .HasForeignKey(d => d.IdCliente)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Proyecto_Cliente");

            entity.HasOne(d => d.IdPMResponsableNavigation).WithMany(p => p.Proyectos)
                .HasForeignKey(d => d.IdPMResponsable)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Proyecto_PM");
        });

        modelBuilder.Entity<RecomendacionBestFit>(entity =>
        {
            entity.ToTable("RecomendacionBestFit");

            entity.Property(e => e.Activo).HasDefaultValue(true);
            entity.Property(e => e.Estado)
                .HasMaxLength(50)
                .HasDefaultValue("Generada");
            entity.Property(e => e.FechaGeneracion)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.PuntajeCompatibilidad).HasColumnType("decimal(5, 2)");

            entity.HasOne(d => d.IdEmpleadoSugeridoNavigation).WithMany(p => p.RecomendacionBestFits)
                .HasForeignKey(d => d.IdEmpleadoSugerido)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_RecomendacionBestFit_Empleado");

            entity.HasOne(d => d.IdTareaNavigation).WithMany(p => p.RecomendacionBestFits)
                .HasForeignKey(d => d.IdTarea)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_RecomendacionBestFit_Tarea");

            entity.HasOne(d => d.IdUsuarioSolicitanteNavigation).WithMany(p => p.RecomendacionBestFits)
                .HasForeignKey(d => d.IdUsuarioSolicitante)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_RecomendacionBestFit_Usuario");
        });

        modelBuilder.Entity<RegistroHora>(entity =>
        {
            entity.ToTable("RegistroHora");

            entity.HasIndex(e => e.IdEmpleado, "IX_RegistroHora_IdEmpleado");

            entity.HasIndex(e => e.IdTarea, "IX_RegistroHora_IdTarea");

            entity.Property(e => e.Activo).HasDefaultValue(true);
            entity.Property(e => e.CantidadHoras).HasColumnType("decimal(5, 2)");
            entity.Property(e => e.Descripcion).HasMaxLength(1000);
            entity.Property(e => e.Fecha).HasPrecision(0);

            entity.HasOne(d => d.IdEmpleadoNavigation).WithMany(p => p.RegistroHoras)
                .HasForeignKey(d => d.IdEmpleado)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_RegistroHora_Empleado");

            entity.HasOne(d => d.IdTareaNavigation).WithMany(p => p.RegistroHoras)
                .HasForeignKey(d => d.IdTarea)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_RegistroHora_Tarea");
        });

        modelBuilder.Entity<Rol>(entity =>
        {
            entity.ToTable("Rol");

            entity.HasIndex(e => e.Nombre, "UQ_Rol_Nombre").IsUnique();

            entity.Property(e => e.Activo).HasDefaultValue(true);
            entity.Property(e => e.Descripcion).HasMaxLength(300);
            entity.Property(e => e.FechaBaja).HasPrecision(0);
            entity.Property(e => e.Nombre).HasMaxLength(80);
        });

        modelBuilder.Entity<RolPermiso>(entity =>
        {
            entity.ToTable("RolPermiso");

            entity.HasIndex(e => new { e.IdRol, e.IdPermiso }, "UQ_RolPermiso_Rol_Permiso").IsUnique();

            entity.HasOne(d => d.IdPermisoNavigation).WithMany(p => p.RolPermisos)
                .HasForeignKey(d => d.IdPermiso)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_RolPermiso_Permiso");

            entity.HasOne(d => d.IdRolNavigation).WithMany(p => p.RolPermisos)
                .HasForeignKey(d => d.IdRol)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_RolPermiso_Rol");
        });

        modelBuilder.Entity<SesionUsuario>(entity =>
        {
            entity.ToTable("SesionUsuario");

            entity.Property(e => e.Activa).HasDefaultValue(true);
            entity.Property(e => e.DireccionIP).HasMaxLength(50);
            entity.Property(e => e.FechaCierre).HasPrecision(0);
            entity.Property(e => e.FechaExpiracion).HasPrecision(0);
            entity.Property(e => e.FechaInicio)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.FechaUltimaActividad).HasPrecision(0);
            entity.Property(e => e.TokenHash).HasMaxLength(500);

            entity.HasOne(d => d.IdUsuarioNavigation).WithMany(p => p.SesionUsuarios)
                .HasForeignKey(d => d.IdUsuario)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_SesionUsuario_Usuario");
        });

        modelBuilder.Entity<SimulacionImpacto>(entity =>
        {
            entity.ToTable("SimulacionImpacto");

            entity.Property(e => e.Activo).HasDefaultValue(true);
            entity.Property(e => e.CargaActual).HasColumnType("decimal(5, 2)");
            entity.Property(e => e.CargaProyectada).HasColumnType("decimal(5, 2)");
            entity.Property(e => e.DisponibilidadRestante).HasColumnType("decimal(5, 2)");
            entity.Property(e => e.FechaCreacion)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.FechaExpiracion).HasPrecision(0);
            entity.Property(e => e.FechaUltimaModificacion).HasPrecision(0);
            entity.Property(e => e.ImpactoOperativo).HasMaxLength(1000);
            entity.Property(e => e.PorcentajeOcupacionActual).HasColumnType("decimal(5, 2)");
            entity.Property(e => e.PorcentajeOcupacionProyectado).HasColumnType("decimal(5, 2)");

            entity.HasOne(d => d.IdEmpleadoCandidatoNavigation).WithMany(p => p.SimulacionImpactos)
                .HasForeignKey(d => d.IdEmpleadoCandidato)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_SimulacionImpacto_Empleado");

            entity.HasOne(d => d.IdTareaNavigation).WithMany(p => p.SimulacionImpactos)
                .HasForeignKey(d => d.IdTarea)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_SimulacionImpacto_Tarea");

            entity.HasOne(d => d.IdUsuarioCreadorNavigation).WithMany(p => p.SimulacionImpactos)
                .HasForeignKey(d => d.IdUsuarioCreador)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_SimulacionImpacto_Usuario");
        });

        modelBuilder.Entity<Skill>(entity =>
        {
            entity.ToTable("Skill");

            entity.HasIndex(e => e.Nombre, "UQ_Skill_Nombre").IsUnique();

            entity.Property(e => e.Activo).HasDefaultValue(true);
            entity.Property(e => e.Categoria).HasMaxLength(100);
            entity.Property(e => e.Nombre).HasMaxLength(100);
        });

        modelBuilder.Entity<Suscripcion>(entity =>
        {
            entity.ToTable("Suscripcion");

            entity.Property(e => e.Activo).HasDefaultValue(true);
            entity.Property(e => e.Estado)
                .HasMaxLength(50)
                .HasDefaultValue("Activa");
            entity.Property(e => e.FechaAlta)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.FechaBaja).HasPrecision(0);
            entity.Property(e => e.FechaProximaRenovacion).HasPrecision(0);
            entity.Property(e => e.FechaVencimiento).HasPrecision(0);
            entity.Property(e => e.ImporteVigente).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.ReferenciaExterna).HasMaxLength(250);
            entity.Property(e => e.RenovacionAutomatica).HasDefaultValue(true);

            entity.HasOne(d => d.IdAgenciaNavigation).WithMany(p => p.Suscripcions)
                .HasForeignKey(d => d.IdAgencia)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Suscripcion_Agencia");

            entity.HasOne(d => d.IdPlanComercialNavigation).WithMany(p => p.Suscripcions)
                .HasForeignKey(d => d.IdPlanComercial)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Suscripcion_PlanComercial");
        });

        modelBuilder.Entity<Tarea>(entity =>
        {
            entity.ToTable("Tarea");

            entity.HasIndex(e => e.IdEmpleadoAsignado, "IX_Tarea_IdEmpleadoAsignado");

            entity.HasIndex(e => e.IdProyecto, "IX_Tarea_IdProyecto");

            entity.Property(e => e.Activo).HasDefaultValue(true);
            entity.Property(e => e.Complejidad).HasMaxLength(50);
            entity.Property(e => e.Deadline).HasPrecision(0);
            entity.Property(e => e.Descripcion).HasMaxLength(1000);
            entity.Property(e => e.Estado).HasMaxLength(50);
            entity.Property(e => e.FechaBaja).HasPrecision(0);
            entity.Property(e => e.FechaFinReal).HasPrecision(0);
            entity.Property(e => e.FechaInicio).HasPrecision(0);
            entity.Property(e => e.HorasEstimadas).HasColumnType("decimal(10, 2)");
            entity.Property(e => e.MotivoBloqueo).HasMaxLength(500);
            entity.Property(e => e.PorcentajeAvance).HasColumnType("decimal(5, 2)");
            entity.Property(e => e.Prioridad).HasMaxLength(50);
            entity.Property(e => e.SeniorityRequerido).HasMaxLength(50);
            entity.Property(e => e.Titulo).HasMaxLength(150);

            entity.HasOne(d => d.IdEmpleadoAsignadoNavigation).WithMany(p => p.Tareas)
                .HasForeignKey(d => d.IdEmpleadoAsignado)
                .HasConstraintName("FK_Tarea_Empleado");

            entity.HasOne(d => d.IdEstadoTareaNavigation).WithMany(p => p.Tareas)
                .HasForeignKey(d => d.IdEstadoTarea)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Tarea_EstadoTarea");

            entity.HasOne(d => d.IdProyectoNavigation).WithMany(p => p.Tareas)
                .HasForeignKey(d => d.IdProyecto)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Tarea_Proyecto");

            entity.HasOne(d => d.IdSkillRequeridoNavigation).WithMany(p => p.Tareas)
                .HasForeignKey(d => d.IdSkillRequerido)
                .HasConstraintName("FK_Tarea_Skill");

            entity.HasOne(d => d.IdTareaPredecesoraNavigation).WithMany(p => p.InverseIdTareaPredecesoraNavigation)
                .HasForeignKey(d => d.IdTareaPredecesora)
                .HasConstraintName("FK_Tarea_Predecessora");
        });

        modelBuilder.Entity<TerminosCondicione>(entity =>
        {
            entity.HasIndex(e => e.Version, "UQ_TerminosCondiciones_Version").IsUnique();

            entity.Property(e => e.FechaVigenciaDesde).HasPrecision(0);
            entity.Property(e => e.FechaVigenciaHasta).HasPrecision(0);
            entity.Property(e => e.Titulo).HasMaxLength(150);
            entity.Property(e => e.Version).HasMaxLength(30);
            entity.Property(e => e.Vigente).HasDefaultValue(true);
        });

        modelBuilder.Entity<Usuario>(entity =>
        {
            entity.ToTable("Usuario");

            entity.HasIndex(e => e.IdAgencia, "IX_Usuario_IdAgencia");

            entity.HasIndex(e => e.IdRol, "IX_Usuario_IdRol");

            entity.HasIndex(e => e.Email, "UQ_Usuario_Email").IsUnique();

            entity.Property(e => e.Activo).HasDefaultValue(true);
            entity.Property(e => e.Apellido).HasMaxLength(100);
            entity.Property(e => e.Email).HasMaxLength(150);
            entity.Property(e => e.Estado)
                .HasMaxLength(50)
                .HasDefaultValue("PendienteValidacion");
            entity.Property(e => e.FechaAlta)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.FechaBaja).HasPrecision(0);
            entity.Property(e => e.Nombre).HasMaxLength(100);
            entity.Property(e => e.PasswordHash).HasMaxLength(500);

            entity.HasOne(d => d.IdAgenciaNavigation).WithMany(p => p.Usuarios)
                .HasForeignKey(d => d.IdAgencia)
                .HasConstraintName("FK_Usuario_Agencia");

            entity.HasOne(d => d.IdRolNavigation).WithMany(p => p.Usuarios)
                .HasForeignKey(d => d.IdRol)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Usuario_Rol");
        });

        modelBuilder.Entity<ValidacionCuentum>(entity =>
        {
            entity.Property(e => e.Activo).HasDefaultValue(true);
            entity.Property(e => e.FechaExpiracion).HasPrecision(0);
            entity.Property(e => e.FechaGeneracion)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.FechaUtilizacion).HasPrecision(0);
            entity.Property(e => e.Metodo).HasMaxLength(30);
            entity.Property(e => e.TokenHash).HasMaxLength(500);

            entity.HasOne(d => d.IdUsuarioNavigation).WithMany(p => p.ValidacionCuenta)
                .HasForeignKey(d => d.IdUsuario)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ValidacionCuenta_Usuario");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
