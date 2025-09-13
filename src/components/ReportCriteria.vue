<script setup lang="ts">
import { computed } from 'vue'
import { useAppStore } from '@/stores/appStore'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Button } from '@/components/ui/button'
import { Badge } from '@/components/ui/badge'
import { Input } from '@/components/ui/input'
import RepoSelector from '@/components/RepoSelector.vue'
import DateRangePicker from '@/components/DateRangePicker.vue'
import type { DateRange } from "reka-ui"
import { getLocalTimeZone, today } from "@internationalized/date"
import { ref } from 'vue'

interface Emits {
  (e: 'generate', criteria: {
    repos: any[],
    releaseBranch: string,
    dateRange: DateRange
  }): void
}

const emit = defineEmits<Emits>()

const store = useAppStore()

// Date range state
const initDateRange = (): DateRange => {
  const end = today(getLocalTimeZone())
  const start = end.subtract({ days: 7 })
  return { start, end }
}

const dateRange = ref<DateRange>(initDateRange())

// Handle date range changes
const handleDateRangeChange = (newRange: DateRange) => {
  dateRange.value = newRange
  if (newRange?.start && newRange?.end) {
    try {
      const fromDate = newRange.start.toDate(getLocalTimeZone())
      const toDate = newRange.end.toDate(getLocalTimeZone())
      store.setDateRange(fromDate, toDate)
    } catch (error) {
      console.warn('Error updating store from range:', error)
    }
  }
}

// Date range presets
const selectPreset = (days: number) => {
  const end = today(getLocalTimeZone())
  const start = end.subtract({ days })
  
  const newRange = { start, end } as DateRange
  dateRange.value = newRange
  handleDateRangeChange(newRange)
}

// Date range validation
const dateRangeDays = computed(() => {
  if (!dateRange.value?.start || !dateRange.value?.end) return 0
  
  try {
    const start = dateRange.value.start.toDate(getLocalTimeZone())
    const end = dateRange.value.end.toDate(getLocalTimeZone())
    const diffTime = Math.abs(end.getTime() - start.getTime())
    const days = Math.ceil(diffTime / (1000 * 60 * 60 * 24))
    return days === 0 ? 1 : days // Minimum 1 day for same-day selection
  } catch (error) {
    console.warn('Error calculating date range days:', error)
    return 0
  }
})

const isDateRangeValid = computed(() => {
  if (!dateRange.value?.start || !dateRange.value?.end) return false
  
  try {
    const start = dateRange.value.start.toDate(getLocalTimeZone())
    const end = dateRange.value.end.toDate(getLocalTimeZone())
    return start <= end && dateRangeDays.value <= 90
  } catch {
    return false
  }
})

// Form validation
const isFormValid = computed(() => {
  return store.selectedRepos.length > 0 && 
         store.releaseBranch.length > 0 &&
         isDateRangeValid.value
})

// Handle form submission
const handleGenerate = () => {
  // Clear any previous errors
  store.clearValidationErrors()
  
  // Validate form
  let isValid = true
  
  // Validate repositories
  if (store.selectedRepos.length === 0) {
    store.setValidationError('repos', 'Please select at least one repository')
    isValid = false
  } else if (store.selectedRepos.length > 10) {
    store.setValidationError('repos', 'Maximum 10 repositories allowed per request')
    isValid = false
  }
  
  // Validate release branch
  const branchRegex = /^release\/\d{4}-\d{2}$/
  if (!store.releaseBranch) {
    store.setValidationError('releaseBranch', 'Release branch is required')
    isValid = false
  } else if (!branchRegex.test(store.releaseBranch)) {
    store.setValidationError('releaseBranch', 'Release branch must match format "release/YYYY-MM"')
    isValid = false
  }
  
  // Validate date range
  if (!isDateRangeValid.value) {
    if (dateRangeDays.value > 90) {
      store.setValidationError('dateRange', 'Date range cannot exceed 90 days')
    } else {
      store.setValidationError('dateRange', 'Please select a valid date range')
    }
    isValid = false
  }
  
  if (isValid) {
    emit('generate', {
      repos: store.selectedRepos,
      releaseBranch: store.releaseBranch,
      dateRange: dateRange.value
    })
  }
}

// Initialize date range in store on mount
handleDateRangeChange(dateRange.value)
</script>

<template>
  <div class="space-y-8">
    <!-- Header -->
    <div class="text-center space-y-4">
      <h1 class="text-3xl font-bold text-primary">Generate Changelog Report</h1>
      <p class="text-muted-foreground max-w-2xl mx-auto">
        Select your repositories, release branch, and date range to generate a comprehensive changelog report
        with Jira issue details and pull request information.
      </p>
    </div>

    <!-- Configuration Form -->
    <div class="max-w-6xl mx-auto">
      <div class="grid grid-cols-1 lg:grid-cols-2 gap-6">
        <!-- Repository Selection -->
        <div>
          <RepoSelector />
        </div>
        
        <!-- Right Column: Release Branch + Date Range -->
        <div class="space-y-6">
          <!-- Release Branch Input -->
          <Card>
            <CardHeader>
              <CardTitle class="text-lg font-semibold">Release Branch</CardTitle>
              <CardDescription>
                Target branch for pull request discovery
              </CardDescription>
            </CardHeader>
            <CardContent class="space-y-4">
              <div>
                <label class="text-sm font-medium text-foreground mb-2 block">
                  Branch Name
                </label>
                <Input
                  v-model="store.releaseBranch"
                  placeholder="release/YYYY-MM"
                  :class="store.errors.releaseBranch ? 'border-destructive' : ''"
                />
                <p class="text-xs text-muted-foreground mt-1">
                  Format: release/YYYY-MM (e.g., release/2025-09)
                </p>
              </div>
              
              <!-- Quick suggestions -->
              <div v-if="store.defaultReleaseBranch">
                <label class="text-sm font-medium text-foreground mb-2 block">
                  Quick Fill
                </label>
                <div class="flex gap-2">
                  <Button
                    variant="outline"
                    size="sm"
                    @click="store.setReleaseBranch(store.defaultReleaseBranch)"
                    class="h-8 text-xs"
                  >
                    {{ store.defaultReleaseBranch }}
                  </Button>
                </div>
              </div>
              
              <!-- Validation Error -->
              <div v-if="store.errors.releaseBranch" class="p-2 bg-destructive/10 border border-destructive/20 rounded">
                <p class="text-xs text-destructive font-medium">{{ store.errors.releaseBranch }}</p>
              </div>
            </CardContent>
          </Card>
          
          <!-- Date Range Selection -->
          <Card>
            <CardHeader>
              <CardTitle class="text-lg font-semibold">Date Range</CardTitle>
              <CardDescription>
                Select the date range for pull request discovery
              </CardDescription>
            </CardHeader>
            <CardContent class="space-y-4">
              <!-- Quick Presets -->
              <div>
                <label class="text-sm font-medium text-foreground mb-2 block">
                  Quick Presets
                </label>
                <div class="flex flex-wrap gap-2">
                  <Button
                    v-for="preset in store.dateRangePresets"
                    :key="preset.label"
                    variant="outline"
                    size="sm"
                    @click="selectPreset(preset.days)"
                    class="h-8"
                  >
                    {{ preset.label }}
                  </Button>
                </div>
              </div>
              
              <!-- Date Range Picker -->
              <div>
                <label class="text-sm font-medium text-foreground mb-2 block">
                  Custom Date Range
                </label>
                <DateRangePicker 
                  v-model="dateRange"
                  @update:model-value="handleDateRangeChange"
                />
              </div>
              
              <!-- Date Range Info -->
              <div class="p-3 rounded-md border border-border bg-muted/30">
                <div class="flex items-center justify-between text-sm">
                  <span class="text-muted-foreground">Duration:</span>
                  <Badge 
                    :variant="isDateRangeValid ? 'secondary' : 'destructive'" 
                    class="px-2"
                  >
                    {{ dateRangeDays }} days
                  </Badge>
                </div>
                
                <!-- Validation warnings -->
                <div v-if="dateRangeDays > 90" class="mt-2 p-2 bg-destructive/10 border border-destructive/20 rounded text-xs text-destructive">
                  ⚠️ Date range exceeds 90-day limit. Please select a shorter period.
                </div>
              </div>
              
              <!-- Validation Error -->
              <div v-if="store.errors.dateRange" class="p-2 bg-destructive/10 border border-destructive/20 rounded">
                <p class="text-xs text-destructive font-medium">{{ store.errors.dateRange }}</p>
              </div>
            </CardContent>
          </Card>
        </div>
      </div>
    </div>

    <!-- Generate Button -->
    <div class="flex justify-center pt-8">
      <Button
        @click="handleGenerate"
        :disabled="!isFormValid"
        size="lg"
        class="h-12 px-12 text-base"
      >
        Generate Changelog Report
      </Button>
    </div>
  </div>
</template>
