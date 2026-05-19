using Microsoft.Extensions.Configuration;

namespace No1.DockerSecretsResolver;

public static class ConfigurationExtensions
{
	public static bool ProcessDockerSecretKey(this IConfiguration configuration, string key) {
		ArgumentNullException.ThrowIfNull(configuration);
		ArgumentException.ThrowIfNullOrWhiteSpace(key);

		var value = configuration[key];

		if (string.IsNullOrWhiteSpace(value)) {
			return false;
		}

		// If value looks like a Docker secret file path
		if (value.StartsWith("/run/secrets/", StringComparison.OrdinalIgnoreCase) && File.Exists(value)) {
			configuration[key] = File.ReadAllText(value).Trim();
			return true;
		} else {
			return false;
		}
	}

	public static void ProcessDockerSecrets(this IConfiguration configuration, bool onlyKeysWithSuffix, params string[]? skipKeys) {
		ArgumentNullException.ThrowIfNull(configuration);

		foreach (var section in configuration.GetChildren()) {
			if (!string.IsNullOrWhiteSpace(section.Value)) {
				if (onlyKeysWithSuffix && !section.Key.EndsWith("_file", StringComparison.OrdinalIgnoreCase)) {
					continue;
				}
				if (skipKeys?.Length > 0 && skipKeys.Contains(section.Key)) {
					continue;
				}
				ProcessDockerSecretKey(configuration, section.Key);
			} else {
				ProcessDockerSecrets(section, onlyKeysWithSuffix, skipKeys);
			}
		}
	}
}