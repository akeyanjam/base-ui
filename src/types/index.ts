// API Contract Types - Must match backend exactly

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

export interface RepoRef {
  projectKey: string
  slug: string
}

export interface StoryInfo {
  issue: JiraIssueInfo
  repo: RepoRef
  sourceBranches: string[]
  pullRequests: PullRequestInfo[]
}

export interface JiraIssueInfo {
  key: string
  summary: string
  status: string
  assignee: string | null
  url: string
  relatedEpics: RelatedEpic[]
}

export interface RelatedEpic {
  key: string
  summary: string | null
  url: string | null
  relationship: string // "Epic Link", "Required by", "Relates"
}

export interface PullRequestInfo {
  id: number
  title: string
  sourceBranch: string
  targetBranch: string
  mergedAt: string
  mergedBy: string | null
  url: string
}

export interface ReportSummary {
  totalStories: number
  repoBreakdown: Record<string, number>
  statusBreakdown: Record<string, number>
}

// Repository metadata response
export interface RepoMetadataResponse {
  repos: RepoRef[]
  defaultReleaseBranch: string
}

// Form & UI Types
export interface FormState {
  selectedRepos: RepoRef[]
  releaseBranch: string
  dateRange: FormDateRange
}

export interface ValidationErrors {
  repos?: string
  releaseBranch?: string
  dateRange?: string
}


// UI State Types
export type ActiveTab = 'results' | 'preview'

export interface AppState {
  // Repository data
  availableRepos: RepoRef[]
  selectedRepos: RepoRef[]
  defaultReleaseBranch: string
  
  // Form state  
  releaseBranch: string
  dateRange: FormDateRange
  
  // Report state
  currentReport: BuildReportResponse | null
  isGenerating: boolean
  
  // UI state
  activeTab: ActiveTab
  errors: ValidationErrors
}

// Date range presets
export interface DateRangePreset {
  label: string
  days: number
}

// Date range type for form components (internal use)
export interface FormDateRange {
  from: Date
  to: Date
}
