using System;
using System.Collections.Generic;
using System.Text;

namespace TeamBalance.MPP
{
    public class TestMPP
    {

        private readonly DAL.Conexion _dal;

        public TestMPP(string cadenaConexion)
        {
            _dal = new DAL.Conexion(cadenaConexion);
        }

        public bool ProbarConexion()
        {
            return _dal.ProbarConexion();
        }

    }
}
