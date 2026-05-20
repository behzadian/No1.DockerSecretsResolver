using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using static System.Collections.Specialized.BitVector32;

namespace No1.DockerSecretsResolver;


[System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1848:Use the LoggerMessage delegates", Justification = "<Pending>")]
[System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1873:Avoid potentially expensive logging", Justification = "<Pending>")]
[System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2254:Template should be a static expression", Justification = "<Pending>")]
public static class ConfigurationExtensions
{
	/// <summary>
	/// Checkes the specified key for docker secret value.
	/// If key was started with `/run/secrets/` and related file exists, loads secret from the file.
	/// If key ends with `_FILE`, then puts the secret in a key without `_FILE` suffix. (ignores the case)
	/// If key doesn't end with `_FILE` suffix, the changes value of itself with the secret
	/// </summary>
	/// <param name="configuration"></param>
	/// <param name="key"></param>
	/// <param name="logger">Can be null, but if not, logs the operations with Trace, Debug, Information levels</param>
	/// <returns></returns>
	public static bool ProcessDockerSecretKey(this IConfiguration configuration, string key, ILogger? logger = null) {
		ArgumentNullException.ThrowIfNull(configuration);
		ArgumentException.ThrowIfNullOrWhiteSpace(key);

		logger?.LogTrace($"Processing key {key} for docker secrets.");

		var value = configuration[key];

		if (string.IsNullOrWhiteSpace(value)) {
			logger?.LogDebug($"Skip parsing key {key}, because value is null or empty");
			return false;
		}

		// If value looks like a Docker secret file path
		if (value.StartsWith("/run/secrets/", StringComparison.OrdinalIgnoreCase)) {
			if (!File.Exists(value)) {
				logger?.LogDebug($"While {key} is similar to docker secret keys, but file `{value}` couldn't be found. Check volumes and key.");
				return false;
			}
			var secret = File.ReadAllText(value).Trim();

			if (key.EndsWith("_FILE", StringComparison.OrdinalIgnoreCase)) {
				var prefixlessKey = key[..^"_FILE".Length];
				logger?.LogInformation($"Replacing value of {prefixlessKey}, with content of file {value}");
				configuration[prefixlessKey] = secret;
			} else {
				logger?.LogInformation($"Replacing value of {key}, with content of file {value}");
				configuration[key] = secret;
			}
			return true;
		} else {
			logger?.LogTrace($"Key {key}'s value is not started with `/run/secrets/`.");
			return false;
		}
	}

	/// <summary>
	/// Checks all keys in configurations, and if find any docker secret, loads and puts it in configurations with below rules:
	/// If key was started with `/run/secrets/` and related file exists, loads secret from the file.
	/// If key ends with `_FILE`, then puts the secret in a key without `_FILE` suffix. (ignores the case)
	/// If key doesn't end with `_FILE` suffix, the changes value of itself with the secret
	/// </summary>
	/// <param name="configuration"></param>
	/// <param name="onlyKeysWithSuffix">Only checks for keys that has `_FILE` suffix.</param>
	/// <param name="loggerFactory">Can be null, but if not, creates a logger with [ConfigurationExtensions] key and logs the operations with Trace, Debug, Information levels</param>
	/// <param name="skipKeys">Keys that being skipped and won't be checked for docker secrets.</param>
	public static void ProcessDockerSecrets(this IConfiguration configuration, bool onlyKeysWithSuffix, ILoggerFactory? loggerFactory = null, params string[]? skipKeys) {
		var logger = loggerFactory?.CreateLogger(typeof(ConfigurationExtensions));
		ProcessDockerSecrets(configuration, onlyKeysWithSuffix, logger, skipKeys);
	}

	/// <summary>
	/// Checks all keys in configurations, and if find any docker secret, loads and puts it in configurations with below rules:
	/// If key was started with `/run/secrets/` and related file exists, loads secret from the file.
	/// If key ends with `_FILE`, then puts the secret in a key without `_FILE` suffix. (ignores the case)
	/// If key doesn't end with `_FILE` suffix, the changes value of itself with the secret
	/// </summary>
	/// <param name="configuration"></param>
	/// <param name="onlyKeysWithSuffix">Only checks for keys that has `_FILE` suffix.</param>
	/// <param name="logger">Can be null, but if not, logs the operations with Trace, Debug, Information levels</param>
	/// <param name="skipKeys">Keys that being skipped and won't be checked for docker secrets.</param>
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