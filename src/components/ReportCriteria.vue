<script setup lang="ts">
import { computed } from 'vue'
import { useAppStore } from '@/stores/appStore'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Button } from '@/components/ui/button'
import { Badge } from '@/components/ui/badge'
import { Input } from '@/components/ui/input'
import { Play } from 'lucide-vue-next'
import RepoSelector from '@/components/RepoSelector.vue'
import DateRangePicker from '@/components/DateRangePicker.vue'
import type { DateRange } from "reka-ui"
import type { FormDateRange } from '@/types'
import { getLocalTimeZone, today } from "@internationalized/date"
import { ref } from 'vue'

interface Emits {
  (e: 'generate', criteria: {
    repos: any[],
    releaseBranch: string,
    dateRange: FormDateRange
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

// Generate date-based release branch suggestions
const generateReleaseBranchSuggestions = () => {
  const now = new Date()
  const currentYear = now.getFullYear()
  const currentMonth = now.getMonth() // 0-based (0 = January, 11 = December)
  
  const suggestions = []
  
  // Past month
  const pastMonth = currentMonth === 0 ? 11 : currentMonth - 1
  const pastYear = currentMonth === 0 ? currentYear - 1 : currentYear
  suggestions.push(`release/${pastYear}-${String(pastMonth + 1).padStart(2, '0')}`)
  
  // Current month
  suggestions.push(`release/${currentYear}-${String(currentMonth + 1).padStart(2, '0')}`)
  
  // Next month
  const nextMonth = currentMonth === 11 ? 0 : currentMonth + 1
  const nextYear = currentMonth === 11 ? currentYear + 1 : currentYear
  suggestions.push(`release/${nextYear}-${String(nextMonth + 1).padStart(2, '0')}`)
  
  return suggestions
}

const releaseBranchSuggestions = generateReleaseBranchSuggestions()

// Initialize release branch with current month (middle suggestion) if not already set
if (!store.releaseBranch) {
  store.setReleaseBranch(releaseBranchSuggestions[1]) // Current month (middle value)
}

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
    // Convert reka-ui DateRange to our FormDateRange type
    if (!dateRange.value.start || !dateRange.value.end) {
      store.setValidationError('dateRange', 'Please select a valid date range')
      return
    }
    
    const convertedDateRange: FormDateRange = {
      from: dateRange.value.start.toDate(getLocalTimeZone()),
      to: dateRange.value.end.toDate(getLocalTimeZone())
    }
    
    emit('generate', {
      repos: store.selectedRepos,
      releaseBranch: store.releaseBranch,
      dateRange: convertedDateRange
    })
  }
}

// Initialize date range in store on mount
if (dateRange.value.start && dateRange.value.end) {
  handleDateRangeChange(dateRange.value as any)
}
</script>

<template>
  <div class="space-y-8">
    <!-- Header -->
    <div class="max-w-6xl mx-auto space-y-4">
      <div class="flex items-center justify-between">
        <h1 class="text-3xl font-bold text-primary">Generate Changelog Report</h1>
        <Button
          @click="handleGenerate"
          :disabled="!isFormValid"
          size="lg"
          class="h-12 px-6 text-base"
        >
          <Play class="w-4 h-4 mr-2" />
          Generate Report
        </Button>
      </div>
      <p class="text-muted-foreground max-w-2xl">
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
              <div>
                <label class="text-sm font-medium text-foreground mb-2 block">
                  Quick Fill
                </label>
                <div class="flex gap-2 flex-wrap">
                  <Button
                    v-for="(suggestion, index) in releaseBranchSuggestions"
                    :key="suggestion"
                    variant="outline"
                    size="sm"
                    @click="store.setReleaseBranch(suggestion)"
                    class="h-8 text-xs"
                    :class="index === 1 ? 'border-primary text-primary' : ''"
                  >
                    {{ suggestion }}
                  </Button>
                </div>
                <p class="text-xs text-muted-foreground mt-1">
                  Suggestions based on current date (past, current, next month)
                </p>
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
              <div class="flex items-start justify-between">
                <div>
                  <CardTitle class="text-lg font-semibold">Date Range</CardTitle>
                  <CardDescription>
                    Select the date range for pull request discovery
                  </CardDescription>
                </div>
                <Badge 
                  :variant="isDateRangeValid ? 'secondary' : 'destructive'" 
                  class="px-2"
                >
                  Duration: {{ dateRangeDays }} days
                </Badge>
              </div>
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
                  :model-value="dateRange as any"
                  @update:model-value="handleDateRangeChange"
                />
              </div>
              
              <!-- Validation warnings -->
              <div v-if="dateRangeDays > 90" class="p-2 bg-destructive/10 border border-destructive/20 rounded text-xs text-destructive">
                ⚠️ Date range exceeds 90-day limit. Please select a shorter period.
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

  </div>
</template>
