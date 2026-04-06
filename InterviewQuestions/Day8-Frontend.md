# Day 8: Frontend Setup (React + TypeScript + Vite)

## Q1: "Why did you choose React + TypeScript + Vite instead of other frameworks or setups?"

### Answer

**React**:

- Most popular UI library with massive ecosystem and job market demand
- Component-based architecture matches Clean Architecture on backend
- Virtual DOM provides excellent performance
- Proven at scale (Facebook, Netflix, Airbnb)

**TypeScript**:

- Type safety catches errors at compile time, not runtime
- Superior IntelliSense and autocomplete in VS Code
- Self-documenting code with interfaces and types
- Refactoring becomes safer (rename, move, extract)
- Essential for large-scale applications

**Vite**:

- **Fast dev server**: ~372ms startup vs. 5-10s with Create React App
- **Instant HMR**: Changes reflect in ~100ms, preserves app state
- **Modern ESM**: No bundling in dev mode, faster iterations
- **Optimized builds**: Uses Rollup for production, tree-shaking built-in
- **Zero config**: TypeScript, JSX, CSS modules work out-of-box

**Why not alternatives?**

- Create React App: Deprecated, slower, webpack-based
- Next.js: Overkill for SPA, adds SSR complexity we don't need
- Angular: Steeper learning curve, more opinionated
- Vue: Smaller job market, less ecosystem

### Key Technical Points

```typescript
// Vite's dev server uses native ES modules
import { Button } from '@/components/Button'  // No bundling in dev!

// Production build still optimizes everything
npm run build  // Rollup bundles, tree-shakes, minifies
```

**Vite's Speed Advantage**:

- Dev server: 372ms (this project) vs. 8000ms (CRA)
- HMR: 50-100ms (Vite) vs. 1000-3000ms (Webpack)
- Cold start: Instant (ESM) vs. Bundle entire app (Webpack)

---

## Q2: "Explain your project structure. Why did you organize it this way?"

### Answer

**Structure**:

```
client/
├── src/
│   ├── features/          # Feature-based modules (auth, resources, bookings)
│   │   ├── auth/          # Login, Register, AuthSlice
│   │   ├── resources/     # Resource CRUD, ResourceSlice
│   │   └── bookings/      # Booking calendar, BookingSlice
│   ├── components/        # Shared UI components (Button, Card, Input)
│   ├── lib/               # Utilities (axios config, cn() function, helpers)
│   ├── store/             # Redux store configuration
│   ├── types/             # TypeScript interfaces (User, Resource, Booking)
│   ├── App.tsx            # Root component, Router setup
│   ├── main.tsx           # React 18+ entry point
│   └── index.css          # TailwindCSS directives
├── public/                # Static assets (favicon, images)
├── index.html             # HTML entry point
├── vite.config.ts         # Vite configuration (plugins, proxy)
├── tsconfig.json          # TypeScript compiler options
├── tailwind.config.js     # TailwindCSS configuration
└── package.json           # Dependencies and scripts
```

**Why Feature-based Structure?**

1. **Scalability**: Each feature is self-contained (components, state, types)
2. **Discoverability**: Easy to find auth-related code (all in `features/auth/`)
3. **Team Collaboration**: Multiple devs can work on different features
4. **Lazy Loading**: Can code-split by feature later (`React.lazy()`)

**Alternative (Not Used)**:

```
src/
├── components/    # ALL components mixed together
├── redux/         # ALL slices mixed together
└── utils/         # ALL utilities mixed together
```

Problem: As app grows, these folders become massive and hard to navigate.

### Key Technical Points

**Path Aliases** (tsconfig.json + vite.config.ts):

```typescript
// Instead of this:
import { Button } from "../../../components/Button";

// We do this:
import { Button } from "@/components/Button";
```

Configuration:

```typescript
// vite.config.ts
resolve: {
  alias: {
    '@': path.resolve(__dirname, './src'),
  },
}

// tsconfig.json
"paths": {
  "@/*": ["./src/*"]
}
```

---

## Q3: "How did you set up TailwindCSS? Why TailwindCSS instead of CSS-in-JS or plain CSS?"

### Answer

**Setup Steps**:

1. **Installed TailwindCSS + PostCSS**:

   ```bash
   npm install -D tailwindcss postcss autoprefixer
   ```

2. **Created `tailwind.config.js`**:

   ```javascript
   export default {
     darkMode: ["class"], // Enable dark mode with class toggle
     content: [
       "./index.html",
       "./src/**/*.{js,ts,jsx,tsx}", // Scan all source files
     ],
     theme: {
       extend: {
         borderRadius: {
           lg: "var(--radius)", // CSS variables for shadcn/ui
           md: "calc(var(--radius) - 2px)",
           sm: "calc(var(--radius) - 4px)",
         },
       },
     },
   };
   ```

3. **Created `postcss.config.js`**:

   ```javascript
   export default {
     plugins: {
       tailwindcss: {},
       autoprefixer: {}, // Auto-add vendor prefixes
     },
   };
   ```

4. **Added directives to `src/index.css`**:

   ```css
   @tailwind base;
   @tailwind components;
   @tailwind utilities;

   @layer base {
     :root {
       --radius: 0.5rem; /* Custom CSS variables */
     }
   }
   ```

**Why TailwindCSS?**

1. **No Context Switching**: Style directly in JSX, no separate CSS files
2. **No Naming Fatigue**: No need to invent class names (`.user-card-header-title-text`)
3. **Dead Code Elimination**: Unused classes are tree-shaken from production build
4. **Consistent Design**: Design tokens (spacing, colors) baked in
5. **Responsive Design**: Built-in breakpoint utilities (`md:`, `lg:`)
6. **Performance**: Only CSS you use is included (~5-10KB gzipped)

**Comparison**:

| Approach    | Pros                          | Cons                                |
| ----------- | ----------------------------- | ----------------------------------- |
| TailwindCSS | Fast, consistent, tree-shaken | HTML looks verbose                  |
| CSS-in-JS   | Scoped, dynamic               | Runtime cost, larger bundle         |
| Plain CSS   | Familiar, simple              | Global scope, naming conflicts      |
| CSS Modules | Scoped, no runtime cost       | Separate files, naming still needed |

**Example**:

```tsx
// TailwindCSS (my choice)
<div className="flex items-center gap-4 p-6 bg-white rounded-lg shadow">
  <h1 className="text-2xl font-bold text-gray-900">Booking System</h1>
</div>;

// CSS-in-JS (styled-components)
const Container = styled.div`
  display: flex;
  align-items: center;
  gap: 1rem;
  padding: 1.5rem;
  background: white;
  border-radius: 0.5rem;
  box-shadow: 0 1px 3px rgba(0, 0, 0, 0.1);
`;
// More code, runtime cost...
```

### Key Technical Points

**Tree-Shaking**:

- TailwindCSS scans all files in `content` array
- Only classes actually used are included in final CSS
- Production build: ~8KB CSS (instead of 3.5MB full Tailwind)

**PurgeCSS** (built into Tailwind v3+):

```javascript
// Don't do this (class won't be detected):
const buttonClass = `bg-${color}-500`; // Dynamic, can't be scanned

// Do this:
const buttonClass = color === "blue" ? "bg-blue-500" : "bg-red-500";
```

---

## Q4: "Explain your Redux Toolkit setup. Why Redux instead of Context API or Zustand?"

### Answer

**Why Redux Toolkit?**

1. **Complex State**: Multi-tenant app with auth, resources, bookings state
2. **Global State**: User, tenant, JWT tokens needed across entire app
3. **DevTools**: Time-travel debugging, action replay, state inspection
4. **Middleware**: Easy to add logging, analytics, API interceptors
5. **TypeScript Support**: Excellent type inference with RTK

**Why Redux Toolkit over plain Redux?**

- **Less Boilerplate**: `createSlice()` auto-generates actions and reducers
- **Immer Built-in**: Mutate state directly (Immer handles immutability)
- **RTK Query**: Built-in data fetching (can replace Axios later)
- **configureStore()**: Automatically sets up Redux DevTools and thunk

**Why not alternatives?**

| Alternative   | Pros                         | Cons                                    |
| ------------- | ---------------------------- | --------------------------------------- |
| Context API   | Built-in, simple             | Re-renders all consumers, no middleware |
| Zustand       | Tiny (1KB), simple           | Less tooling, smaller ecosystem         |
| Recoil        | Atomic state, flexible       | Smaller community, more experimental    |
| Redux Toolkit | DevTools, middleware, proven | Slightly more boilerplate than Zustand  |

**When I'd choose alternatives**:

- **Context API**: Small apps, 1-2 global values (theme, locale)
- **Zustand**: Medium apps, no need for DevTools
- **Redux Toolkit**: Large apps, complex workflows, need debugging

### Key Technical Points

**Redux Toolkit Slice Example**:

```typescript
// features/auth/authSlice.ts
import { createSlice, PayloadAction } from "@reduxjs/toolkit";

interface AuthState {
  user: User | null;
  tenant: Tenant | null;
  accessToken: string | null;
  refreshToken: string | null;
  isAuthenticated: boolean;
}

const initialState: AuthState = {
  user: null,
  tenant: null,
  accessToken: null,
  refreshToken: null,
  isAuthenticated: false,
};

const authSlice = createSlice({
  name: "auth",
  initialState,
  reducers: {
    // Redux Toolkit uses Immer, so we can "mutate" state
    setCredentials: (
      state,
      action: PayloadAction<{
        user: User;
        tenant: Tenant;
        accessToken: string;
        refreshToken: string;
      }>,
    ) => {
      state.user = action.payload.user;
      state.tenant = action.payload.tenant;
      state.accessToken = action.payload.accessToken;
      state.refreshToken = action.payload.refreshToken;
      state.isAuthenticated = true;
    },
    logout: (state) => {
      state.user = null;
      state.tenant = null;
      state.accessToken = null;
      state.refreshToken = null;
      state.isAuthenticated = false;
    },
  },
});

export const { setCredentials, logout } = authSlice.actions;
export default authSlice.reducer;
```

**Store Configuration**:

```typescript
// store/index.ts
import { configureStore } from "@reduxjs/toolkit";
import authReducer from "@/features/auth/authSlice";
import resourcesReducer from "@/features/resources/resourcesSlice";
import bookingsReducer from "@/features/bookings/bookingsSlice";

export const store = configureStore({
  reducer: {
    auth: authReducer,
    resources: resourcesReducer,
    bookings: bookingsReducer,
  },
});

export type RootState = ReturnType<typeof store.getState>;
export type AppDispatch = typeof store.dispatch;
```

**TypeScript Hooks**:

```typescript
// store/hooks.ts
import { TypedUseSelectorHook, useDispatch, useSelector } from "react-redux";
import type { RootState, AppDispatch } from "./index";

export const useAppDispatch = () => useDispatch<AppDispatch>();
export const useAppSelector: TypedUseSelectorHook<RootState> = useSelector;
```

Usage:

```tsx
// Instead of:
const user = useSelector((state: RootState) => state.auth.user);

// We do:
const user = useAppSelector((state) => state.auth.user); // Fully typed!
```

---

## Q5: "How does your Vite proxy work? Why proxy API calls instead of direct requests?"

### Answer

**Configuration** (vite.config.ts):

```typescript
export default defineConfig({
  server: {
    port: 3000,
    proxy: {
      "/api": {
        target: "http://localhost:5036", // Backend API
        changeOrigin: true,
      },
    },
  },
});
```

**How it Works**:

```typescript
// Frontend runs on http://localhost:3000
// Backend runs on http://localhost:5036

// When you make a request to:
axios.get('/api/v1/auth/login')

// Vite intercepts and proxies it to:
http://localhost:5036/api/v1/auth/login

// Browser thinks it's same-origin request ✓
```

**Why Proxy?**

1. **Avoid CORS Issues**: Browser sees same-origin request (`localhost:3000` → `localhost:3000/api`)
2. **Simplify Config**: No need to configure CORS on backend for development
3. **Production-like Setup**: In production, NGINX/reverse proxy does the same thing
4. **Cleaner Code**: No environment variable for API base URL in dev mode

**How to Use**:

```typescript
// Axios base configuration (lib/axios.ts)
import axios from "axios";

const axiosInstance = axios.create({
  baseURL: "/api", // All requests start with /api
  headers: {
    "Content-Type": "application/json",
  },
});

// Add interceptors for JWT tokens
axiosInstance.interceptors.request.use((config) => {
  const token = localStorage.getItem("accessToken");
  if (token) {
    config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
});

export default axiosInstance;
```

**Without Proxy** (what we avoid):

```typescript
// Backend needs CORS configuration:
app.UseCors((policy) =>
  policy
    .WithOrigins("http://localhost:3000") // Must whitelist frontend
    .AllowAnyMethod()
    .AllowAnyHeader(),
);

// Frontend needs full URL:
axios.get("http://localhost:5036/api/v1/auth/login"); // Ugly, hardcoded
```

### Key Technical Points

**Production Setup** (no proxy):

```typescript
// vite.config.ts (production build doesn't use proxy)
// We'll use environment variables:

const API_URL = import.meta.env.VITE_API_URL || '/api'

// .env.production
VITE_API_URL=https://api.bookingsystem.com

// Deployment: NGINX reverse proxy
location /api {
  proxy_pass http://backend:5036;
}
```

**changeOrigin: true** explained:

- Changes `Host` header to match target
- Required when backend checks Host header
- Our backend doesn't check, but good practice

---

## Q6: "How did you handle the limitations of Vite's create-vite CLI? What did you learn?"

### Answer

**Problem Encountered**:

```bash
npm create vite@latest client -- --template react-ts
# Still prompted for framework selection (interactive)

npm create vite@latest client -- --template react-ts --yes
# Still prompted (--yes flag ignored)

$env:VITE_CREATE_SKIP_PROMPT='true'; npm create vite@latest -- client --template react-ts
# Still prompted (environment variable not recognized)
```

**Root Cause**:

- Vite CLI (create-vite v9.0.4) requires interactive terminal input
- No fully non-interactive mode available
- Documentation suggests flags work, but they don't in practice

**Solution (Manual Setup)**:

1. **Create folder and package.json**: `npm init -y`
2. **Install dependencies manually**: React, Vite, TypeScript, types
3. **Create config files**: vite.config.ts, tsconfig.json, index.html
4. **Create source files**: main.tsx, App.tsx, index.css
5. **Update package.json**: Change to ESM, add scripts

**What I Learned**:

1. **Tooling Limitations**: Even popular tools have automation gaps
2. **ESM vs CommonJS**: `"type": "module"` required for Vite (uses import/export)
3. **\_\_dirname in ESM**: Not available, must use `fileURLToPath(import.meta.url)`
4. **Manual Setup Benefits**: Full control, understand every config option
5. **Problem-Solving**: When automation fails, break task into smaller steps

**\_\_dirname Issue**:

```typescript
// CommonJS (old way)
const __dirname = __dirname; // Automatic global

// ESM (Vite requires this)
import path from "path";
import { fileURLToPath } from "url";

const __dirname = path.dirname(fileURLToPath(import.meta.url));
```

### Key Technical Points

**ESM vs CommonJS**:

| Feature              | CommonJS (`require`)     | ESM (`import`)              |
| -------------------- | ------------------------ | --------------------------- |
| Syntax               | `const x = require('x')` | `import x from 'x'`         |
| Top-level await      | ❌ Not supported         | ✅ Supported                |
| Tree-shaking         | ❌ Difficult             | ✅ Built-in                 |
| Browser support      | ❌ Needs bundler         | ✅ Native (modern browsers) |
| **dirname/**filename | ✅ Automatic globals     | ❌ Must construct manually  |
| Vite compatibility   | ⚠️ Workarounds needed    | ✅ First-class support      |

**Why Vite Requires ESM**:

- Dev server serves modules directly (no bundling)
- Browser imports via `<script type="module">`
- Native ES modules enable instant HMR

---

## Q7: "Explain shadcn/ui. Why use it instead of Material-UI or Ant Design?"

### Answer

**What is shadcn/ui?**

- **NOT a component library** (no npm package called `shadcn`)
- **Collection of copy-paste components** (you own the code)
- **Built on Radix UI** (unstyled, accessible primitives)
- **Styled with TailwindCSS** (utility classes)

**How It Works**:

```bash
npx shadcn@latest init    # One-time setup
npx shadcn@latest add button  # Copies Button.tsx to src/components/ui/
```

Result: You get `src/components/ui/button.tsx` in YOUR codebase.

**Why shadcn/ui?**

1. **Full Control**: Component code is in your repo, modify freely
2. **No Bundle Size Impact**: Only copy components you actually use
3. **No Breaking Changes**: Updating is opt-in (you decide when to update component)
4. **Customizable**: TailwindCSS makes styling modifications easy
5. **Accessible**: Built on Radix UI (ARIA compliant)
6. **Modern**: Uses latest React patterns (Server Components compatible)

**Comparison**:

| Library     | Install Method  | Bundle Size | Customization | Breaking Changes |
| ----------- | --------------- | ----------- | ------------- | ---------------- |
| shadcn/ui   | Copy to project | Only used   | Full control  | Never (you own)  |
| Material-UI | npm package     | Large       | Theme API     | Major versions   |
| Ant Design  | npm package     | Very large  | Theme API     | Major versions   |
| Chakra UI   | npm package     | Medium      | Good          | Major versions   |

**Why not Material-UI?**

- Large bundle size (~300KB minified)
- Harder to customize (theme API, CSS-in-JS)
- Breaking changes between major versions

**Why not Ant Design?**

- Enterprise-focused, opinionated design
- Even larger bundle size
- Less modern (older React patterns)

### Key Technical Points

**shadcn/ui Setup**:

```bash
npm install class-variance-authority clsx tailwind-merge lucide-react
```

**Installed**:

- `class-variance-authority`: Variant-based styling
- `clsx` + `tailwind-merge`: Utility for merging classNames
- `lucide-react`: Icon library

**cn() Utility** (src/lib/utils.ts):

```typescript
import { clsx, type ClassValue } from "clsx";
import { twMerge } from "tailwind-merge";

export function cn(...inputs: ClassValue[]) {
  return twMerge(clsx(inputs));
}
```

Usage:

```tsx
import { cn } from "@/lib/utils";

<button
  className={cn(
    "px-4 py-2 rounded", // Base styles
    isActive && "bg-blue-500", // Conditional
    className, // Allow prop override
  )}
/>;
```

**Example Button Component** (from shadcn/ui):

```tsx
import { cva, type VariantProps } from "class-variance-authority";

const buttonVariants = cva(
  "inline-flex items-center justify-center rounded-md text-sm font-medium transition-colors",
  {
    variants: {
      variant: {
        default: "bg-primary text-white hover:bg-primary/90",
        destructive: "bg-red-500 text-white hover:bg-red-600",
        outline: "border border-input bg-background hover:bg-accent",
      },
      size: {
        default: "h-10 px-4 py-2",
        sm: "h-9 rounded-md px-3",
        lg: "h-11 rounded-md px-8",
      },
    },
    defaultVariants: {
      variant: "default",
      size: "default",
    },
  },
);

export interface ButtonProps extends VariantProps<typeof buttonVariants> {
  // ...
}
```

Usage:

```tsx
<Button variant="destructive" size="lg">
  Delete
</Button>
```

---

## Bonus: Production Improvements for Frontend

### 1. **Environment Variables**

```typescript
// .env.local (gitignored)
VITE_API_URL=http://localhost:5036/api

// .env.production
VITE_API_URL=https://api.bookingsystem.com/api

// Access in code:
const API_URL = import.meta.env.VITE_API_URL
```

**Security Note**: Only variables prefixed with `VITE_` are exposed to client!

### 2. **Code Splitting**

```tsx
import { lazy, Suspense } from 'react'

// Split by feature
const ResourcesPage = lazy(() => import('@/features/resources/ResourcesPage'))
const BookingsPage = lazy(() => import('@/features/bookings/BookingsPage'))

<Suspense fallback={<Loading />}>
  <Route path="/resources" element={<ResourcesPage />} />
</Suspense>
```

**Result**: Initial bundle ~50KB, features load on-demand.

### 3. **Error Boundaries**

```tsx
class ErrorBoundary extends React.Component {
  state = { hasError: false };

  static getDerivedStateFromError() {
    return { hasError: true };
  }

  componentDidCatch(error, info) {
    console.error("ErrorBoundary caught:", error, info);
  }

  render() {
    if (this.state.hasError) {
      return <ErrorFallback />;
    }
    return this.props.children;
  }
}
```

### 4. **React Query** (instead of Redux for server state)

```tsx
import { useQuery } from "@tanstack/react-query";

const { data, isLoading } = useQuery({
  queryKey: ["resources"],
  queryFn: () => api.get("/v1/resources"),
});
```

**Benefits**: Automatic caching, refetching, optimistic updates.

### 5. **PWA Support**

```bash
npm install -D vite-plugin-pwa
```

```typescript
// vite.config.ts
import { VitePWA } from "vite-plugin-pwa";

plugins: [
  react(),
  VitePWA({
    registerType: "autoUpdate",
    manifest: {
      name: "Booking System",
      short_name: "Booking",
      theme_color: "#3b82f6",
    },
  }),
];
```

**Result**: Installable web app, offline support, fast loading.

### 6. **Bundle Analysis**

```bash
npm run build
npx vite-bundle-visualizer
```

Identify large dependencies, optimize imports.

### 7. **Lighthouse CI**

```yaml
# .github/workflows/lighthouse.yml
- name: Run Lighthouse
  uses: treosh/lighthouse-ci-action@v9
  with:
    urls: http://localhost:3000
    uploadArtifacts: true
```

Track performance, accessibility, SEO scores.

---

## Summary

**Day 8 Achievements**:

- ✅ React 19 + TypeScript 6 + Vite 8 setup
- ✅ TailwindCSS 4 with shadcn/ui compatibility
- ✅ Redux Toolkit 2 state management foundation
- ✅ Feature-based project structure
- ✅ Path aliases and TypeScript strict mode
- ✅ Vite proxy for seamless API integration
- ✅ ESM modules with proper \_\_dirname handling

**Key Technologies Explained**:

- Vite: Fast dev server, instant HMR, modern build tool
- TypeScript: Type safety, better DX, fewer bugs
- TailwindCSS: Utility-first CSS, tree-shaken, consistent
- Redux Toolkit: Global state, DevTools, middleware
- shadcn/ui: Copy-paste components, full control

**Next Steps (Day 9+)**:

- Authentication UI (Login, Register forms)
- Redux slices for auth, resources, bookings
- Axios interceptors for JWT tokens
- React Router setup with protected routes
- Form validation with React Hook Form + Zod
- shadcn/ui components (Button, Card, Input, Label)
