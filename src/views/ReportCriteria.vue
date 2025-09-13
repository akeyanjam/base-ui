<script setup lang="ts">
import { onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { useAppStore } from '@/stores/appStore'
import { useApi } from '@/composables/useApi'
import { toast } from 'vue-sonner'
import ReportCriteria from '@/components/ReportCriteria.vue'
import type { DateRange } from "reka-ui"
import { getLocalTimeZone } from "@internationalized/date"

const router = useRouter()
const store = useAppStore()
const { fetchRepos, generateReport } = useApi()

// Load repositories on mount
onMounted(async () => {
  if (store.availableRepos.length === 0) {
    await loadRepositories()
  }
})

// Load repositories from API
const loadRepositories = async () => {
  try {
    const repoData = await fetchRepos()
    store.setAvailableRepos(repoData.repos)
    store.setDefaultReleaseBranch(repoData.defaultReleaseBranch)
    
    toast('Repositories Loaded', {
      description: `Found ${repoData.repos.length} available repositories`
    })
  } catch (error) {
    console.error('Failed to load repositories:', error)
    toast('Failed to Load Repositories', {
      description: error instanceof Error ? error.message : 'Unknown error occurred'
    })
  }
}

// Handle report generation
const handleGenerate = async (criteria: {
  repos: any[],
  releaseBranch: string,
  dateRange: DateRange
}) => {
  store.setIsGenerating(true)
  store.setCurrentReport(null)
  
  try {
    // Build API request
    const request = {
      repos: criteria.repos,
      releaseBranch: criteria.releaseBranch,
      from: criteria.dateRange.start.toDate(getLocalTimeZone()).toISOString(),
      to: criteria.dateRange.end.toDate(getLocalTimeZone()).toISOString()
    }
    
    // Navigate to results page immediately to show loading
    await router.push('/results')
    
    // Generate report
    const report = await generateReport(request)
    
    // Set report in store
    store.setCurrentReport(report)
    store.setActiveTab('results')
    
    // Show success message
    toast('Report Generated Successfully', {
      description: `Found ${report.summary.totalStories} stories across ${Object.keys(report.summary.repoBreakdown).length} repositories`
    })
    
  } catch (error) {
    console.error('Failed to generate report:', error)
    toast('Report Generation Failed', {
      description: error instanceof Error ? error.message : 'Unknown error occurred'
    })
  } finally {
    store.setIsGenerating(false)
  }
}
</script>

<template>
  <div class="min-h-screen bg-background">
    <!-- Header Section -->
    <div class="bg-primary text-primary-foreground">
      <div class="container mx-auto px-6 py-12">
        <div class="max-w-4xl">
          <h1 class="text-4xl font-bold mb-4">MBSS Changelog Builder</h1>
          <h2 class="text-2xl font-light mb-6">Automated Release Report Generation</h2>
          <p class="text-lg opacity-90 mb-8 leading-relaxed">
            Generate comprehensive changelog reports by analyzing merged pull requests 
            and enriching with Jira issue information. Please select the repositories you want to include in the report.
          </p>
          
          <!-- Status Indicator -->
          <div class="flex items-center gap-4">
            <div class="flex items-center gap-2">
              <div class="flex items-center gap-1">
                <span class="text-sm opacity-90">{{ store.availableRepos.length }} repositories available</span>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>

    <!-- Main Content -->
    <div class="container mx-auto px-6 py-8">
      <ReportCriteria @generate="handleGenerate" />
    </div>
  </div>
</template>
