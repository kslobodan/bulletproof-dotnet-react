# Day 8: Frontend Setup (React + TypeScript + Vite)

**Steps: 1-22**

---

## Manual Vite Project Creation

**Note**: Vite CLI `create-vite` required interactive prompts that couldn't be automated. Proceeded with manual project setup.

1. Created client folder and initialized project:
   - `mkdir client; cd client`
   - `npm init -y`

2. Installed React core dependencies:
   - `npm install react react-dom`
   - React v19.2.4, React-DOM v19.2.4

3. Installed Vite and TypeScript tooling:
   - `npm install -D vite @vitejs/plugin-react typescript @types/react @types/react-dom`
   - Vite v8.0.5, TypeScript v6.0.2

4. Updated package.json:
   - Changed `"type"` from "commonjs" to "module"
   - Added scripts: `dev` (vite), `build` (tsc && vite build), `preview` (vite preview)
   - Added `"private": true`

5. Created vite.config.ts:
   - React plugin: @vitejs/plugin-react
   - Path alias: @ → ./src
   - Dev server: port 3000
   - Proxy: /api → http://localhost:5036 (backend API)

6. Created tsconfig.json:
   - Target: ES2020
   - Module: ESNext
   - JSX: react-jsx (automatic runtime)
   - Strict mode: enabled
   - Path mapping: @/_ → ./src/_

7. Created tsconfig.node.json:
   - TypeScript config for Vite config file
   - Module resolution: bundler

8. Created index.html (entry point):
   - HTML5 structure
   - div#root for React mounting
   - script tag: type="module" src="/src/main.tsx"

## TailwindCSS Setup

9. Installed TailwindCSS and PostCSS:
   - `npm install -D tailwindcss postcss autoprefixer`
   - TailwindCSS v4.x, PostCSS, Autoprefixer

10. Created tailwind.config.js:
    - Dark mode: class-based
    - Content: index.html and src/\*_/_.{js,ts,jsx,tsx}
    - Theme extensions: CSS variables for border-radius (shadcn/ui compatible)

11. Created postcss.config.js:
    - Plugins: tailwindcss, autoprefixer

12. Created src/index.css:
    - TailwindCSS directives: @tailwind base, components, utilities
    - CSS custom properties: --radius

## React Source Files

13. Created src/main.tsx:
    - React 18+ entry point with createRoot API
    - StrictMode wrapper
    - Imports: React, ReactDOM, index.css, App

14. Created src/App.tsx:
    - Root functional component
    - Basic UI: "Booking System" heading with TailwindCSS classes
    - Verified TailwindCSS working (bg-gray-50, text-4xl, etc.)

15. Created src/vite-env.d.ts:
    - TypeScript reference types for Vite client

## Core Libraries Installation

16. Installed Redux Toolkit and React Router:
    - `npm install @reduxjs/toolkit react-redux react-router-dom axios`
    - @reduxjs/toolkit v2.x (state management)
    - react-redux v9.x (React bindings for Redux)
    - react-router-dom v7.x (routing)
    - axios v1.x (HTTP client)

17. Installed shadcn/ui peer dependencies:
    - `npm install class-variance-authority clsx tailwind-merge lucide-react`
    - class-variance-authority: variant-based styling
    - clsx + tailwind-merge: className utility
    - lucide-react: icon library

18. Created src/lib/utils.ts:
    - cn() utility function for combining class names with clsx and tailwind-merge
    - Used by shadcn/ui components

## Project Structure

19. Created folder structure:
    - src/features/ (auth, resources, bookings)
    - src/components/ (shared UI components)
    - src/store/ (Redux store configuration)
    - src/types/ (TypeScript interfaces)
    - src/lib/ (utilities)

20. Created .gitignore for client:
    - node_modules, dist, dist-ssr
    - logs (\*.log)
    - editor files (.vscode, .idea, .DS_Store)
    - \*.local

## Verification

21. Started Vite dev server:
    - Command: `npm run dev`
    - Server running on http://localhost:3000

22. **Package Summary**:
    - Total packages: 77 audited
    - Vulnerabilities: 0
    - Key dependencies:
      - React v19.2.4
      - Vite v8.0.5
      - TypeScript v6.0.2
      - TailwindCSS v4.x
      - Redux Toolkit v2.x
      - React Router v7.x
      - Axios v1.x
