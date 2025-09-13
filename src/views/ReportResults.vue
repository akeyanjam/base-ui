<script setup lang="ts">
import { useRouter } from 'vue-router'
import { useAppStore } from '@/stores/appStore'
import ReportResults from '@/components/ReportResults.vue'

const router = useRouter()
const store = useAppStore()

// Handle start over - navigate back to criteria
const handleStartOver = async () => {
  // Reset any relevant state if needed
  store.setCurrentReport(null)
  store.clearValidationErrors()
  
  // Navigate back to criteria page
  await router.push('/')
}
</script>

<template>
  <div class="min-h-screen bg-background">
    <!-- Header Section -->
    <div class="bg-primary text-primary-foreground">
      <div class="container mx-auto px-6 py-8">
        <div class="max-w-4xl">
          <h1 class="text-3xl font-bold mb-2">Report Results</h1>
          <p class="text-lg opacity-90 leading-relaxed">
            Your changelog report for <strong>{{ store.currentReport?.releaseBranch || 'the selected release' }}</strong>
          </p>
        </div>
      </div>
    </div>

    <!-- Main Content -->
    <div class="container mx-auto px-6 py-8">
      <ReportResults @start-over="handleStartOver" />
    </div>
  </div>
</template>
