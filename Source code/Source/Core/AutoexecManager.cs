using System;
using System.IO;
using System.Threading.Tasks;

namespace Null.Core
{
    public class AutoexecManager
    {
        private readonly VelocityAPI.VelAPI _velocity;
        private readonly string _autoexecPath;
        private bool _isProcessing = false;

        public AutoexecManager(VelocityAPI.VelAPI velocity)
        {
            _velocity = velocity;
            _autoexecPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Autoexec");
            
            if (!Directory.Exists(_autoexecPath))
            {
                Directory.CreateDirectory(_autoexecPath);
            }
        }

        public async Task RunAutoexec()
        {
            if (_isProcessing) return;
            _isProcessing = true;

            try
            {
                string[] files = Directory.GetFiles(_autoexecPath, "*.lua");
                foreach (string file in files)
                {
                    try
                    {
                        string script = File.ReadAllText(file);
                        if (!string.IsNullOrWhiteSpace(script))
                        {
                            _velocity.Execute(script);
                        }
                    }
                    catch (Exception)
                    {
                        // Log or handle individual file errors
                    }
                }
            }
            finally
            {
                _isProcessing = false;
            }
        }
    }
}
