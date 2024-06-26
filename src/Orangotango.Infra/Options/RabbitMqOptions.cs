﻿namespace Orangotango.Infra.Options;

public class RabbitMqOptions
{
    public const string RabbitMQ = "RabbitMQ";

    public string ConnectionString { get; set; }
    public string HostName { get; set; }
    public string VirtualHost { get; set; }
    public string Username { get; set; }
    public string Password { get; set; }

    public bool IsValid()
        => (!string.IsNullOrWhiteSpace(HostName) && !string.IsNullOrWhiteSpace(VirtualHost)) || HasConnectionString();

    public bool HasConnectionString()
        => !string.IsNullOrWhiteSpace(ConnectionString);
}
