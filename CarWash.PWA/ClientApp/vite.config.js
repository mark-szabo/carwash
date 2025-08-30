import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';
import eslint from 'vite-plugin-eslint';

export default defineConfig(() => {
    return {
        plugins: [react(), eslint()],
        server: {
            port: 51145,
            open: true,
        },
        build: {
            outDir: 'build',
        },
    };
});
