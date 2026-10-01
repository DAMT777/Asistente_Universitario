// Los límites entre capas se verifican aquí: lo reutilizable en React Native
// no puede depender del DOM, del router ni de la UI web.
const SIN_REACT = ['react', 'react/*', 'react-dom', 'react-dom/*', '@tanstack/*'];
const SIN_WEB = ['react-dom', 'react-dom/*', 'react-router', 'react-router-dom'];
const SIN_UI = ['@/ui', '@/ui/*', '@/features', '@/features/*', '@/app', '@/app/*'];

module.exports = {
  root: true,
  parser: '@typescript-eslint/parser',
  plugins: ['@typescript-eslint', 'react-hooks'],
  extends: ['eslint:recommended', 'plugin:@typescript-eslint/recommended', 'plugin:react-hooks/recommended'],
  env: { browser: true, es2022: true },
  overrides: [
    {
      files: ['src/types/**', 'src/schemas/**', 'src/domain/**', 'src/theme/**'],
      rules: { 'no-restricted-imports': ['error', { patterns: [...SIN_REACT, ...SIN_WEB, ...SIN_UI, '@/api', '@/api/*', '@/hooks', '@/hooks/*'] }] },
    },
    {
      files: ['src/api/**'],
      rules: { 'no-restricted-imports': ['error', { patterns: [...SIN_REACT, ...SIN_WEB, ...SIN_UI, '@/hooks', '@/hooks/*'] }] },
    },
    {
      files: ['src/hooks/**'],
      rules: { 'no-restricted-imports': ['error', { patterns: [...SIN_WEB, ...SIN_UI] }] },
    },
  ],
};
