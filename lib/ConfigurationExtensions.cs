using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using static System.Collections.Specialized.BitVector32;

namespace No1.DockerSecretsResolver;


[System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1848:Use the LoggerMessage delegates", Justification = "<Pending>")]
[System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1873:Avoid potentially expensive logging", Justification = "<Pending>")]
[System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2254:Template should be a static expression", Justification = "<Pending>")]
public static class ConfigurationExtensions
{
	public static bool ProcessDockerSecretKey(this IConfiguration configuration, string key, ILogger? logger = null) {
		ArgumentNullException.ThrowIfNull(configuration);
		ArgumentException.ThrowIfNullOrWhiteSpace(key);

		var value = configuration[key];

		if (string.IsNullOrWhiteSpace(value)) {
			logger?.LogDebug($"Skip parsing key {key}, because value is null or empty");
			return false;
		}

		// If value looks like a Docker secret file path
		if (value.StartsWith("/run/secrets/", StringComparison.OrdinalIgnoreCase) && File.Exists(value)) {
			logger?.LogInformation($"Replacing value of {key}, with content of file {value}");
			configuration[key] = File.ReadAllText(value).Trim();

			if (key.EndsWith("_FILE", StringComparison.OrdinalIgnoreCase)) {
				var prefixlessKey = key[..^"_FILE".Length];
				logger?.LogInformation($"Replacing value of {prefixlessKey}, with content of file {value}");
			}
			return true;
		} else {
			return false;
		}
	}
	public static void ProcessDockerSecrets(this IConfiguration configuration, bool onlyKeysWithSuffix, ILoggerFactory? loggerFactory = null, params string[]? skipKeys) {
		var logger = loggerFactory?.CreateLogger(typeof(ConfigurationExtensions));
		ProcessDockerSecrets(configuration, onlyKeysWithSuffix, logger, skipKeys);
	}

	public static void ProcessDockerSecrets(this IConfiguration configuration, bool onlyKeysWithSuffix, ILogger? logger = null, params string[]? skipKeys) {
		ArgumentNullException.ThrowIfNull(configuration);
		logger?.LogTrace($"Processing configuration");

		foreach (var section in configuration.GetChildren()) {
			if (!string.IsNullOrWhiteSpace(section.Value)) {
				if (onlyKeysWithSuffix && !section.Key.EndsWith("_file", StringComparison.OrdinalIgnoreCase)) {
					logger?.LogDebug($"Skip parsing key {section.Key}, because not ending with '_FILE'");
					continue;
				}
				if (skipKeys?.Length > 0 && skipKeys.Contains(section.Key)) {
					logger?.LogDebug($"Skip parsing key {section.Key}, because key is skipped");
					continue;
				}
				ProcessDockerSecretKey(configuration, section.Key, logger);
			} else {
				ProcessDockerSecrets(section, onlyKeysWithSuffix, logger, skipKeys);
			}
		}
	}
}