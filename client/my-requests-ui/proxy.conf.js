// Network routing only. Authentication is supplied by the Angular interceptor.
module.exports = {
  '/api': {
    target: 'http://localhost:5098',
    secure: false,
    changeOrigin: true,
  },
};
