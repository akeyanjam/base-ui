# SIT Changelog Builder - Frontend Architecture Document
**(Vue 3 + Vite + Tailwind + shadcn-vue + TypeScript)**

---

## 1. System Overview

### 1.1 Purpose
Single Page Application providing an intuitive interface for scrum masters to generate changelog reports. Users select repositories, release branches, and date ranges, then get a formatted markdown report they can copy to Outlook/email.

### 1.2 Technology Stack
- **Framework**: Vue 3 (Composition API + `<script setup>`)
- **Build Tool**: Vite 5+
- **Styling**: Tailwind CSS 3+
- **UI Components**: shadcn-vue (pre-installed)
- **Icons**: lucide-vue-next
- **Language**: TypeScript 5+
- **Markdown**: markdown-it
- **State Management**: Pinia (single store)
- **HTTP Client**: fetch API with mock JSON fallback

### 1.3 Component System
**shadcn-vue components are already installed**. Additional components can be added with:
```bash
npx shadcn-vue@latest add [component-name]
```

Available components shown in sidebar: badge, button, calendar, card, input, popover, range-calendar, separator, table, tabs, etc.

---

## 2. User Experience Flow

### 2.1 Primary User Journey
1. **Landing**: User sees form with repository checkboxes, release branch input, date range pickers
2. **Configuration**: 
   - Select repos (multi-select with "Select All" option)
   - Choose release branch (text input with current/next month suggestions)
   - Pick date range (quick presets: 7 days, 2 weeks, 1 month + custom date pickers)
3. **Generation**: Click "Generate Report" → loading state → results appear
4. **Review**: Switch between "Results" (table view) and "Preview" (rendered markdown) tabs
5. **Copy**: Click "Copy Report" button → content copied to clipboard for pasting in Outlook

### 2.2 UX Principles
- **Immediate Feedback**: Real-time validation, loading states, toast notifications
- **Progressive Disclosure**: Show configuration first, results after generation
- **Keyboard Friendly**: Tab navigation, Enter to submit, Escape to close modals
- **Mobile Responsive**: Works on tablets, gracefully degrades on phones
- **Accessibility**: Proper ARIA labels, screen reader support, high contrast

### 2.3 Error Handling Strategy
- **Validation Errors**: Show inline under form fields as user types
- **API Errors**: Toast notification + error alert in results area
- **Network Issues**: Retry button with exponential backoff
- **Empty States**: Clear messaging when no results found

---

## 3. Project Structure (Simplified)

```
src/
├── components/
│   ├── ui/                    # shadcn-vue components (pre-installed)
│   ├── RepoSelector.vue       # Multi-select with checkboxes
│   ├── DateRangePicker.vue    # Preset buttons + calendar pickers  
│   ├── ResultsTable.vue       # Expandable story rows
│   ├── MarkdownPreview.vue    # Rendered HTML output
│   └── LoadingSpinner.vue     # Custom loading component
├── composables/
│   ├── useApi.ts             # API calls (with JSON fallback)
│   ├── useMarkdown.ts        # markdown-it integration
│   └── useClipboard.ts       # Navigator clipboard API
├── stores/
│   └── appStore.ts           # Single Pinia store
├── types/
│   └── index.ts              # All TypeScript interfaces
├── utils/
│   └── constants.ts          # API endpoints, date formats
├── public/
│   └── mock-data.json        # Development data (temporary)
├── App.vue                   # Root component
└── main.ts                   # Entry point
```

---

## 4. Core Data Flow & Integration Points

### 4.1 State Management (Single Store)
```typescript
// stores/appStore.ts - Single Pinia store
interface AppState {
  // Repository data
  availableRepos: RepoRef[]
  selectedRepos: RepoRef[]
  defaultReleaseBranch: string
  
  // Form state  
  releaseBranch: string
  dateRange: { from: Date, to: Date }
  
  // Report state
  currentReport: BuildReportResponse | null
  isGenerating: boolean
  
  // UI state
  activeTab: 'results' | 'preview'
  errors: Record<string, string>
}
```

### 4.2 API Integration Strategy

**Development Mode**: Fetch from JSON file in `/public/mock-data.json`
**Production Mode**: Actual API calls (initially commented out)

```typescript
// composables/useApi.ts
export function useApi() {
  const fetchRepos = async () => {
    // TODO: Uncomment for production
    // return await fetch('/api/meta/repos').then(r => r.json())
    
    // Development: Load from public folder
    return await fetch('/mock-data.json').then(r => r.json())
  }
  
  const generateReport = async (request: BuildReportRequest) => {
    // TODO: Uncomment for production  
    // return await fetch('/api/report/build', { 
    //   method: 'POST', 
    //   body: JSON.stringify(request) 
    // }).then(r => r.json())
    
    // Development: Simulate delay + return mock data
    await new Promise(resolve => setTimeout(resolve, 2000))
    return mockReportResponse
  }
}
```

### 4.3 Critical Integration Points

**Form Validation**: Real-time validation using computed properties
- Repository selection (min 1, max 10)
- Release branch format (`release/YYYY-MM`)
- Date range (from < to, max 90 days)

**Markdown Rendering**: Use markdown-it to convert report data to HTML
- Table formatting for story details
- Clickable links to Jira/Bitbucket  
- Syntax highlighting for code blocks

**Clipboard Integration**: Navigator Clipboard API with fallback
- Copy rendered HTML (not raw markdown)
- Toast notification on success/failure
- Fallback for unsupported browsers

---

## 5. Component Architecture

### 5.1 Main Layout Structure
```
App.vue
└── ReportGenerator.vue (Single Page)
    ├── Card: "Report Configuration" 
    │   ├── RepoSelector.vue (checkbox grid)
    │   ├── div: Release Branch Input + Suggestion Buttons
    │   ├── DateRangePicker.vue (presets + calendars)
    │   └── GenerateButton.vue (loading states)
    ├── Tabs: "Results" | "Preview" (conditional render)
    │   ├── TabsContent: "results"  
    │   │   ├── ReportSummary.vue (metrics badges)
    │   │   └── ResultsTable.vue (expandable rows)
    │   └── TabsContent: "preview"
    │       ├── MarkdownPreview.vue (rendered HTML)
    │       └── CopyButton.vue (clipboard integration)
    └── Toaster.vue (global notifications)
```

### 5.2 Key Component Responsibilities

**RepoSelector**: 
- Fetch repos on mount from store
- Multi-select with "Select All" toggle
- Show project/repo structure clearly
- Real-time validation feedback

**DateRangePicker**:
- Quick preset buttons (7d, 2w, 1m)
- Two calendar popovers (from/to dates)
- Prevent invalid ranges (from > to)
- Highlight active preset

**ResultsTable**:
- Collapsible rows showing story details
- Epic links, PR links, branch names
- Status badges, assignee info
- Sort by repo, status, or assignee

**MarkdownPreview**:
- Render markdown as styled HTML
- Responsive table layout
- Copy button in header
- Loading skeleton while rendering

---

## 6. TypeScript Interfaces (Critical)

### 6.1 API Contract Types
```typescript
// types/index.ts - Must match backend exactly

export interface BuildReportRequest {
  repos: RepoRef[]
  releaseBranch: string  
  from: string // ISO 8601 UTC
  to: string   // ISO 8601 UTC
}

export interface BuildReportResponse {
  releaseBranch: string
  from: string
  to: string
  generatedAt: string
  stories: StoryInfo[]
  summary: ReportSummary
}

export interface StoryInfo {
  issue: {
    key: string
    summary: string
    status: string
    assignee: string | null
    url: string
    relatedEpics: Array<{
      key: string
      summary: string | null
      url: string | null
      relationship: string // "Epic Link", "Required by", "Relates"
    }>
  }
  repo: RepoRef
  sourceBranches: string[]
  pullRequests: Array<{
    id: number
    title: string
    mergedAt: string
    mergedBy: string | null
    url: string
  }>
}

export interface RepoRef {
  projectKey: string
  slug: string
}
```

### 6.2 Form & UI Types
```typescript
export interface FormState {
  selectedRepos: RepoRef[]
  releaseBranch: string
  dateRange: { from: Date, to: Date }
}

export interface ValidationErrors {
  repos?: string
  releaseBranch?: string
  dateRange?: string
}

export interface ToastMessage {
  type: 'success' | 'error' | 'warning'
  title: string
  description?: string
}
```

---

## 7. Development Data Setup

### 7.1 Mock Data Structure
Create `/public/mock-data.json` with realistic sample data:

```json
{
  "repos": [
    { "projectKey": "PAY", "slug": "payments-api" },
    { "projectKey": "PAY", "slug": "settlements-engine" },
    { "projectKey": "CORE", "slug": "platform-services" }
  ],
  "defaultReleaseBranch": "release/2025-09",
  "sampleReport": {
    "releaseBranch": "release/2025-09",
    "stories": [
      {
        "issue": {
          "key": "MBBO-1234",
          "summary": "Enable multi-bank payment routing",
          "status": "Done",
          "assignee": "John Smith",
          "url": "https://jira.company.com/browse/MBBO-1234",
          "relatedEpics": [
            {
              "key": "PAY-EP-45", 
              "summary": "Payment Gateway Redesign",
              "relationship": "Epic Link"
            }
          ]
        },
        "repo": { "projectKey": "PAY", "slug": "payments-api" },
        "sourceBranches": ["feature/MBBO-1234-multi-bank"],
        "pullRequests": [
          {
            "id": 147,
            "title": "MBBO-1234: Implement routing logic", 
            "mergedAt": "2025-09-10T14:22:33.000Z",
            "url": "https://bitbucket.company.com/projects/PAY/repos/payments-api/pull-requests/147"
          }
        ]
      }
    ],
    "summary": {
      "totalStories": 8,
      "repoBreakdown": { "PAY/payments-api": 5, "CORE/platform-services": 3 },
      "statusBreakdown": { "Done": 6, "In QA": 2 }
    }
  }
}
```

---

## 8. UX Patterns & Behavior

### 8.1 Loading States
- **Initial Load**: Skeleton placeholders for repo list
- **Report Generation**: Progress spinner with descriptive text ("Fetching pull requests...", "Loading Jira details...", "Generating report...")  
- **Markdown Rendering**: Shimmer effect while processing

### 8.2 Interactive Patterns
- **Form Validation**: Show validation errors on blur, clear on valid input
- **Repo Selection**: Visual feedback for selected items, count indicator
- **Date Presets**: Highlight active preset, smooth transitions
- **Tab Switching**: Instant switching with persistent state

### 8.3 Responsive Design
- **Desktop**: Side-by-side configuration and results
- **Tablet**: Stacked cards with full-width tables  
- **Mobile**: Single column, collapsible sections, touch-friendly controls

### 8.4 Accessibility Features
- **Keyboard Navigation**: Tab order, Enter/Space activation
- **Screen Reader**: Proper ARIA labels, live regions for dynamic content
- **High Contrast**: Respect system preference, sufficient color contrast
- **Focus Management**: Clear focus indicators, logical tab sequence

---

## 9. Performance Considerations

### 9.1 Optimization Strategies
- **Virtual Scrolling**: If story count exceeds 100 items
- **Computed Caching**: Cache expensive markdown rendering
- **Debounced Search**: If adding repository filtering
- **Lazy Loading**: Load markdown renderer only when needed

### 9.2 Bundle Management
- **Code Splitting**: Separate chunks for markdown renderer
- **Tree Shaking**: Import only needed utilities
- **Asset Optimization**: Compress images, use modern formats

---

## 10. Implementation Priorities

### 10.1 Phase 1 (Core Functionality)
1. Basic form with repo selection and date pickers
2. Mock API integration with JSON file
3. Results table with story expansion
4. Markdown preview with copy functionality
5. Basic error handling and validation

### 10.2 Phase 2 (Polish & Production)
1. Replace mock data with real API calls
2. Advanced loading states and animations
3. Keyboard shortcuts and accessibility improvements  
4. Mobile responsive optimization
5. Performance monitoring and optimization

### 10.3 Development Notes
- Start with shadcn-vue components for consistent styling
- Use TypeScript strictly - no `any` types
- Implement composables for reusable logic
- Test with realistic data volumes (50+ stories)
- Focus on copy-paste workflow optimization

---

This architecture emphasizes user experience, maintainable code structure, and smooth integration points while keeping implementation details flexible for the development team.