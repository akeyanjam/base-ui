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
      from: criteria.dateRange.start!.toDate(getLocalTimeZone()).toISOString(),
      to: criteria.dateRange.end!.toDate(getLocalTimeZone()).toISOString()
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
      <div class="container mx-auto px-6 py-8">
        <div class="max-w-4xl flex flex-col items-start gap-4">
          <!-- Logo -->
          <div class="flex-shrink-0">
            <img src="/bank-of-america-logo.svg" alt="MBSS Logo" class="h-6 w-auto object-contain" />
          </div>
          
          <!-- Header Content -->
          <div class="w-full">
            <h1 class="text-3xl font-bold mb-2">BofA Point of Sale Changelog Report Builder</h1>
            
            <!-- Status Indicator -->
            <div class="flex items-center gap-2 mt-3">
              <span class="text-sm opacity-90">{{ store.availableRepos.length }} repositories available</span>
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
