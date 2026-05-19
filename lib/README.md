# No1.DockerSecretsUpdater

Reads all settings and place that value of docker secrets in the configurations.

This library provides 2 methods to check if a key is a docker secrets and puts the secret in the configuration.

- ConfigurationExtensions.ProcessDockerSecrets. (or `configuration.ProcessDockerSecrets` if you import `using No1.DockerSecretsResolver;`)

This method reads all keys in configurations, and if the key has not been skipped, processes it with `ProcessDockerSecretKey` method.


- ProcessDockerSecretKey (or `configuration.ProcessDockerSecretKey` if you import `using No1.DockerSecretsResolver;`)

This method processes specific key and if:

-- Key's value started with `/run/secrets/`
-- Specified file in value exists

Then reads the file and put secret in:

-- The key itself, if key does not end with `_FILE` (ignores case) suffix. (such as Connection::ConnectionString)
-- Another key without `_FILE` suffix, if key ends with `_FILE` (ignores case) suffix. (so if the key was `Connection::ConnectionString_FILE`, the secret will be placed in `Connection::ConnectionString`)