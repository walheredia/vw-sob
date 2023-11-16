using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Serilog;

namespace vwsob
{
    public class LoggerConfig
    {
        public ILogger CreateLogger()
        {
            return new LoggerConfiguration()
            .WriteTo.File("logs/log.txt", rollingInterval: RollingInterval.Minute, retainedFileCountLimit: 5000)
            .CreateLogger();
        }

    }

    public class MyService
    {
        private readonly ILogger _logger;

        public MyService(ILogger logger)
        {
            _logger = logger;
        }
    }
}
