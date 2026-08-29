using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text;

namespace TeamBalance.DAL
{
    public class Conexion
    {
        private readonly string _cadenaConexion;

        public Conexion(string cadenaConexion)
        {
            _cadenaConexion = cadenaConexion;
        }

        public bool ProbarConexion()
        {
            try
            {
                using SqlConnection conn = new SqlConnection(_cadenaConexion);
                conn.Open();
                return conn.State == ConnectionState.Open;
            }
            catch { return false; }
        }

        public DataTable Leer(string procedimiento, List<SqlParameter>? parametros = null)
        {
            try
            {
                DataTable dataTable = new DataTable();
                using SqlConnection conn = new SqlConnection(_cadenaConexion);
                using SqlCommand cmd = new SqlCommand(procedimiento, conn);
                cmd.CommandType = CommandType.StoredProcedure;

                if (parametros != null)
                {
                    foreach (SqlParameter parametro in parametros)
                    {
                        cmd.Parameters.Add(parametro);
                    }
                }

                conn.Open();
                using SqlDataAdapter adapter = new SqlDataAdapter(cmd);
                adapter.Fill(dataTable);
                return dataTable;
            }
            catch(SqlException ex) { throw new Exception(ex.Message); }
            catch (Exception ex) { throw new Exception(ex.Message); }
        }

        public bool Escribir(string procedimiento, List<SqlParameter>? parametros = null)
        {
            try
            {
                using SqlConnection conn = new SqlConnection(_cadenaConexion);
                using SqlCommand cmd = new SqlCommand(procedimiento, conn);
                cmd.CommandType = CommandType.StoredProcedure;

                if (parametros != null)
                {
                    foreach (SqlParameter parametro in parametros)
                    {
                        cmd.Parameters.Add(parametro);
                    }
                }

                conn.Open();
                cmd.ExecuteNonQuery();
                return true;
            }
            catch(SqlException ex) { throw new Exception(ex.Message);}
            catch (Exception ex) { throw new Exception(ex.Message); }
        
        }
    }
}
