//-----------------------------------------------------------------------------
// Filename: TestLogger.cs
//
// Description: Logger utility for VP8 test vector unit tests.
//
// Author(s):
// Copilot
//
// History:
// 23 Jun 2025	Copilot	Created, Dublin, Ireland.
//
// License: 
// BSD 3-Clause "New" or "Revised" License, see included LICENSE.md file.
//-----------------------------------------------------------------------------

using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Extensions.Logging;

namespace VP8.Net.TestVectors
{
    /// <summary>
    /// Logger utility for VP8 test vector unit tests.
    /// </summary>
    public class TestLogger
    {
        /// <summary>
        /// Creates a logger factory for test output.
        /// </summary>
        /// <param name="output">The xunit test output helper.</param>
        /// <returns>A configured logger factory.</returns>
        public static ILoggerFactory GetLogger(Xunit.Abstractions.ITestOutputHelper output)
        {
            string template = "{Timestamp:HH:mm:ss.ffff} [{Level}] {Scope} {Message}{NewLine}{Exception}";
            
            var serilog = new LoggerConfiguration()
                .MinimumLevel.Is(Serilog.Events.LogEventLevel.Debug)
                .Enrich.WithProperty("ThreadId", System.Threading.Thread.CurrentThread.ManagedThreadId)
                .WriteTo.Console(outputTemplate: template)
                .CreateLogger();
                
            return new SerilogLoggerFactory(serilog);
        }
    }
}