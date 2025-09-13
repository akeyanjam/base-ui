<script setup lang="ts">
import { computed, ref, watch } from 'vue'
import { useAppStore } from '@/stores/appStore'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Button } from '@/components/ui/button'
import { Badge } from '@/components/ui/badge'
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs'
import ResultsTable from '@/components/ResultsTable.vue'
import MarkdownPreview from '@/components/MarkdownPreview.vue'
import { 
  ArrowLeftIcon,
  LoaderIcon,
  CheckCircleIcon,
  AlertCircleIcon
} from 'lucide-vue-next'

interface Emits {
  (e: 'startOver'): void
}

const emit = defineEmits<Emits>()

const store = useAppStore()

// Loading steps for better UX
const loadingSteps = ref<string[]>([])

// Watch for generation start to show loading steps
watch(() => store.isGenerating, (isGenerating) => {
  if (isGenerating) {
    loadingSteps.value = []
    simulateLoadingSteps()
  } else {
    loadingSteps.value = []
  }
})

// Simulate loading steps for better UX
const simulateLoadingSteps = async () => {
  const steps = [
    'Validating request parameters...',
    'Querying Bitbucket for merged pull requests...',
    'Extracting Jira keys from branch names...',
    'Fetching Jira issue details...',
    'Resolving related epics...',
    'Building report data...',
    'Finalizing report...'
  ]
  
  for (let i = 0; i < steps.length && store.isGenerating; i++) {
    loadingSteps.value.push(steps[i])
    await new Promise(resolve => setTimeout(resolve, 400))
  }
}

// Computed properties
const currentStepNumber = computed(() => loadingSteps.value.length)
const totalSteps = 7

const hasResults = computed(() => store.currentReport !== null)

// Handle start over
const handleStartOver = () => {
  // Reset store state
  store.setCurrentReport(null)
  store.clearValidationErrors()
  store.setActiveTab('results')
  
  // Emit to parent
  emit('startOver')
}
</script>

<template>
  <div class="space-y-8">
    <!-- Loading State -->
    <div v-if="store.isGenerating" class="max-w-2xl mx-auto">
      <Card>
        <CardHeader class="text-center">
          <div class="flex items-center justify-center gap-3 mb-4">
            <LoaderIcon class="h-8 w-8 animate-spin text-primary" />
            <div>
              <CardTitle class="text-xl font-semibold">Generating Report</CardTitle>
              <CardDescription class="mt-1">
                Processing your request ({{ currentStepNumber }}/{{ totalSteps }})
              </CardDescription>
            </div>
          </div>
        </CardHeader>
        <CardContent>
          <div class="space-y-3">
            <div v-for="(step, index) in loadingSteps" :key="index" class="flex items-center gap-3">
              <div class="w-2 h-2 rounded-full bg-primary animate-pulse"></div>
              <span class="text-sm text-foreground">{{ step }}</span>
            </div>
          </div>
          
          <!-- Progress Bar -->
          <div class="mt-6 w-full bg-muted rounded-full h-2">
            <div 
              class="bg-primary h-2 rounded-full transition-all duration-300" 
              :style="{ width: `${(currentStepNumber / totalSteps) * 100}%` }"
            ></div>
          </div>
        </CardContent>
      </Card>
    </div>

    <!-- Results State -->
    <div v-else-if="hasResults" class="space-y-6">
      <!-- Header with Start Over Button -->
      <div class="flex items-center justify-between">
        <div class="flex items-center gap-3">
          <CheckCircleIcon class="h-6 w-6 text-green-600" />
          <div>
            <h1 class="text-2xl font-bold text-primary">Report Generated Successfully</h1>
            <p class="text-muted-foreground">
              Found {{ store.currentReport.summary.totalStories }} stories across {{ Object.keys(store.currentReport.summary.repoBreakdown).length }} repositories
            </p>
          </div>
        </div>
        
        <Button variant="outline" @click="handleStartOver" class="flex items-center gap-2">
          <ArrowLeftIcon class="h-4 w-4" />
          Start Over
        </Button>
      </div>

      <!-- Report Details -->
      <div class="grid grid-cols-1 md:grid-cols-3 gap-4 p-4 rounded-lg border border-border bg-muted/30">
        <div class="text-center">
          <div class="text-2xl font-bold text-primary">{{ store.currentReport.summary.totalStories }}</div>
          <div class="text-sm text-muted-foreground">Total Stories</div>
        </div>
        <div class="text-center">
          <div class="text-2xl font-bold text-primary">{{ Object.keys(store.currentReport.summary.repoBreakdown).length }}</div>
          <div class="text-sm text-muted-foreground">Repositories</div>
        </div>
        <div class="text-center">
          <div class="text-2xl font-bold text-primary">{{ Object.keys(store.currentReport.summary.statusBreakdown).length }}</div>
          <div class="text-sm text-muted-foreground">Different Statuses</div>
        </div>
      </div>

      <!-- Results Tabs -->
      <Tabs v-model="store.activeTab" class="space-y-6">
        <div class="flex items-center justify-center">
          <TabsList class="grid w-full max-w-md grid-cols-2">
            <TabsTrigger value="results">
              Results View
            </TabsTrigger>
            <TabsTrigger value="preview">
              Markdown Preview
            </TabsTrigger>
          </TabsList>
        </div>
        
        <TabsContent value="results" class="space-y-6">
          <ResultsTable />
        </TabsContent>
        
        <TabsContent value="preview" class="space-y-6">
          <MarkdownPreview />
        </TabsContent>
      </Tabs>
    </div>

    <!-- Error State -->
    <div v-else class="max-w-2xl mx-auto">
      <Card>
        <CardHeader class="text-center">
          <div class="flex items-center justify-center gap-3 mb-4">
            <AlertCircleIcon class="h-8 w-8 text-destructive" />
            <div>
              <CardTitle class="text-xl font-semibold">Something went wrong</CardTitle>
              <CardDescription class="mt-1">
                Unable to generate the report. Please try again.
              </CardDescription>
            </div>
          </div>
        </CardHeader>
        <CardContent class="text-center">
          <Button @click="handleStartOver" class="flex items-center gap-2">
            <ArrowLeftIcon class="h-4 w-4" />
            Start Over
          </Button>
        </CardContent>
      </Card>
    </div>
  </div>
</template>
