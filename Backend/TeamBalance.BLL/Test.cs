using System;
using System.Collections.Generic;
using System.Text;
using TeamBalance.MPP;

namespace TeamBalance.BLL
{
    public class TestBLL
    {
        private readonly TestMPP _mpp;

        public TestBLL(string cadenaConexion)
        {
            _mpp = new TestMPP(cadenaConexion);
        }

        public bool ProbarConexion()
        {
            return _mpp.ProbarConexion();
        }
    }
}
