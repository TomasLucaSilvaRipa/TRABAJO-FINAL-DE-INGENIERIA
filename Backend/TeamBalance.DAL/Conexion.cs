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
            catch (SqlException)
            {
                throw;
            }
            catch (Exception)
            {
                throw;
            }
        }

        public DataTable Leer(string procedimiento, List<SqlParameter>? parametros = null)
        {
            DataTable dataTable = new DataTable();
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
                using SqlDataAdapter adapter = new SqlDataAdapter(cmd);
                adapter.Fill(dataTable);
            }
            catch (SqlException ex){ throw new Exception(ex.Message); }
            catch (Exception ex){ throw new Exception(ex.Message); }
            return dataTable;
        }

        public bool Escribir(
            string procedimiento,
            List<SqlParameter>? parametros = null)
        {
            using SqlConnection conn = new SqlConnection(_cadenaConexion);

            conn.Open();

            using SqlTransaction transaccion = conn.BeginTransaction();

            try
            {
                using SqlCommand cmd = new SqlCommand(procedimiento, conn, transaccion);
                cmd.CommandType = CommandType.StoredProcedure;

                if (parametros != null)
                {
                    foreach (SqlParameter parametro in parametros)
                    {
                        cmd.Parameters.Add(parametro);
                    }
                }

                cmd.ExecuteNonQuery();
                transaccion.Commit();
                return true;
            }
            catch (SqlException) { transaccion.Rollback(); return false; }
            catch (Exception) { transaccion.Rollback(); return false; }
        }
    }
}
