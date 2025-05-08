module.exports = function override(config) {
  config.module.rules.unshift({
    test: /\.worker\.ts$/,
    use: { loader: 'worker-loader' },
  });
  return config;
};