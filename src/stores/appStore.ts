import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import type { 
  AppState, 
  RepoRef, 
  BuildReportResponse, 
  ValidationErrors, 
  ActiveTab,
  DateRangePreset 
} from '@/types'

export const useAppStore = defineStore('app', () => {
  // Repository data
  const availableRepos = ref<RepoRef[]>([])
  const selectedRepos = ref<RepoRef[]>([])
  const defaultReleaseBranch = ref<string>('release/2025-09')
  
  // Form state
  const releaseBranch = ref<string>('')
  const dateRange = ref<{ from: Date, to: Date }>({
    from: new Date(Date.now() - 7 * 24 * 60 * 60 * 1000), // 7 days ago
    to: new Date()
  })
  
  // Report state
  const currentReport = ref<BuildReportResponse | null>(null)
  const isGenerating = ref<boolean>(false)
  
  // UI state
  const activeTab = ref<ActiveTab>('results')
  const errors = ref<ValidationErrors>({})
  
  // Computed properties
  const isFormValid = computed(() => {
    return selectedRepos.value.length > 0 && 
           releaseBranch.value.length > 0 &&
           dateRange.value.from < dateRange.value.to
  })
  
  const hasResults = computed(() => {
    return currentReport.value !== null
  })
  
  const selectedRepoCount = computed(() => selectedRepos.value.length)
  
  // Date range presets
  const dateRangePresets: DateRangePreset[] = [
    { label: 'Last 7 days', days: 7 },
    { label: 'Last 14 days', days: 14 },
    { label: 'Last 30 days', days: 30 }
  ]
  
  // Actions
  function setAvailableRepos(repos: RepoRef[]) {
    availableRepos.value = repos
  }
  
  function setDefaultReleaseBranch(branch: string) {
    defaultReleaseBranch.value = branch
    if (!releaseBranch.value) {
      releaseBranch.value = branch
    }
  }
  
  function toggleRepoSelection(repo: RepoRef) {
    const index = selectedRepos.value.findIndex(
      r => r.projectKey === repo.projectKey && r.slug === repo.slug
    )
    
    if (index >= 0) {
      selectedRepos.value.splice(index, 1)
    } else {
      selectedRepos.value.push(repo)
    }
    
    // Clear repo validation error if repos are selected
    if (selectedRepos.value.length > 0 && errors.value.repos) {
      delete errors.value.repos
    }
  }
  
  function selectAllRepos() {
    selectedRepos.value = [...availableRepos.value]
    if (errors.value.repos) {
      delete errors.value.repos
    }
  }
  
  function clearRepoSelection() {
    selectedRepos.value = []
  }
  
  function setReleaseBranch(branch: string) {
    releaseBranch.value = branch
    
    // Clear validation error if branch format is valid
    const branchRegex = /^release\/\d{4}-\d{2}$/
    if (branchRegex.test(branch) && errors.value.releaseBranch) {
      delete errors.value.releaseBranch
    }
  }
  
  function setDateRange(from: Date, to: Date) {
    dateRange.value = { from, to }
    
    // Clear validation error if range is valid
    if (from < to && errors.value.dateRange) {
      delete errors.value.dateRange
    }
  }
  
  function setDateRangePreset(preset: DateRangePreset) {
    const to = new Date()
    const from = new Date(Date.now() - preset.days * 24 * 60 * 60 * 1000)
    setDateRange(from, to)
  }
  
  function setCurrentReport(report: BuildReportResponse | null) {
    currentReport.value = report
  }
  
  function setIsGenerating(generating: boolean) {
    isGenerating.value = generating
  }
  
  function setActiveTab(tab: ActiveTab) {
    activeTab.value = tab
  }
  
  function setValidationError(field: keyof ValidationErrors, message: string) {
    errors.value[field] = message
  }
  
  function clearValidationErrors() {
    errors.value = {}
  }
  
  
  // Reset form to initial state
  function resetForm() {
    selectedRepos.value = []
    releaseBranch.value = defaultReleaseBranch.value
    dateRange.value = {
      from: new Date(Date.now() - 7 * 24 * 60 * 60 * 1000),
      to: new Date()
    }
    clearValidationErrors()
  }
  
  // Reset entire store
  function reset() {
    availableRepos.value = []
    selectedRepos.value = []
    defaultReleaseBranch.value = 'release/2025-09'
    releaseBranch.value = ''
    dateRange.value = {
      from: new Date(Date.now() - 7 * 24 * 60 * 60 * 1000),
      to: new Date()
    }
    currentReport.value = null
    isGenerating.value = false
    activeTab.value = 'results'
    errors.value = {}
  }
  
  return {
    // State
    availableRepos,
    selectedRepos,
    defaultReleaseBranch,
    releaseBranch,
    dateRange,
    currentReport,
    isGenerating,
    activeTab,
    errors,
    dateRangePresets,
    
    // Computed
    isFormValid,
    hasResults,
    selectedRepoCount,
    
    // Actions
    setAvailableRepos,
    setDefaultReleaseBranch,
    toggleRepoSelection,
    selectAllRepos,
    clearRepoSelection,
    setReleaseBranch,
    setDateRange,
    setDateRangePreset,
    setCurrentReport,
    setIsGenerating,
    setActiveTab,
    setValidationError,
    clearValidationErrors,
    resetForm,
    reset
  }
})
