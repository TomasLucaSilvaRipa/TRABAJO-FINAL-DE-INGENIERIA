namespace TeamBalance.BE.Entidades;

public class GestionProyectoOpciones
{
    public GestionProyectoOpciones(List<Cliente> clientes, List<Usuario> responsables)
    {
        Clientes = clientes;
        Responsables = responsables;
    }
    public List<Cliente> Clientes { get; set; }
    public List<Usuario> Responsables { get; set; }
}
