﻿using Microsoft.AspNetCore;

public class Program
{
    public static Task Main()
    {
        var webHostBuilder = WebHost.CreateDefaultBuilder();
        var hostBuilder = webHostBuilder.UseStartup<Startup>();
        return hostBuilder.Build().RunAsync();
    }
}