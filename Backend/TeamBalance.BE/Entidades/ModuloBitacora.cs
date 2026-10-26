using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace TeamBalance.BE.Entidades;
public enum ModuloBitacora
{
    General = 1,
    Contratacion = 2,
    PreguntasFrecuentes = 3,
    HelpDesk = 4,
    Kanban = 5,
    Novedades = 6,
    Planes = 7,
    Planificacion = 8,
    Proyectos = 9,
    Recursos = 10,
    Registro = 11,
    Respaldos = 12,
    Seguridad = 13,
    Suscripcion = 14,
    Usuarios = 15,
    Operadores = 16,
    Encuestas = 17
}
