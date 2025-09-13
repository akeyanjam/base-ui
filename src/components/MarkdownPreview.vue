<script setup lang="ts">
import { ref, computed, watch } from 'vue'
import { useAppStore } from '@/stores/appStore'
import { useMarkdown } from '@/composables/useMarkdown'
import { useClipboard } from '@/composables/useClipboard'
import { toast } from 'vue-sonner'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Button } from '@/components/ui/button'
import { Badge } from '@/components/ui/badge'
import { Separator } from '@/components/ui/separator'
import { 
  CopyIcon, 
  CheckIcon, 
  DownloadIcon,
  LoaderIcon 
} from 'lucide-vue-next'

const store = useAppStore()
const { generateMarkdown, renderMarkdown, generateHtmlReport } = useMarkdown()
const { copyToClipboard, copyHtmlToClipboard, isLoading: isCopyLoading, error: copyError } = useClipboard()

// State
const isGenerating = ref(false)
const copySuccess = ref<'markdown' | 'html' | null>(null)
const htmlContent = ref('')
const markdownContent = ref('')

// Generate content when report changes
watch(() => store.currentReport, async (report) => {
  if (!report) {
    htmlContent.value = ''
    markdownContent.value = ''
    return
  }
  
  isGenerating.value = true
  
  try {
    // Generate markdown
    markdownContent.value = generateMarkdown(report)
    
    // Render HTML with small delay for better UX
    await new Promise(resolve => setTimeout(resolve, 300))
    htmlContent.value = renderMarkdown(markdownContent.value)
  } catch (error) {
    console.error('Error generating markdown:', error)
    toast('Preview Generation Failed', {
      description: 'Unable to generate markdown preview'
    })
  } finally {
    isGenerating.value = false
  }
}, { immediate: true })

// Copy functions
const copyMarkdown = async () => {
  if (!markdownContent.value) return
  
  const success = await copyToClipboard(markdownContent.value)
  if (success) {
    copySuccess.value = 'markdown'
    toast('Copied!', {
      description: 'Markdown content copied to clipboard'
    })
    
    // Clear success indicator after delay
    setTimeout(() => {
      if (copySuccess.value === 'markdown') copySuccess.value = null
    }, 2000)
  } else {
    toast('Copy Failed', {
      description: copyError.value || 'Unable to copy to clipboard'
    })
  }
}

const copyHtml = async () => {
  if (!htmlContent.value) return
  
  const success = await copyHtmlToClipboard(htmlContent.value, markdownContent.value)
  if (success) {
    copySuccess.value = 'html'
    toast('Copied!', {
      description: 'Changelog copied to clipboard (paste into Outlook/Word)'
    })
    
    // Clear success indicator after delay
    setTimeout(() => {
      if (copySuccess.value === 'html') copySuccess.value = null
    }, 2000)
  } else {
    toast('Copy Failed', {
      description: copyError.value || 'Unable to copy HTML to clipboard'
    })
  }
}

// Download functions
const downloadMarkdown = () => {
  if (!markdownContent.value || !store.currentReport) return
  
  const blob = new Blob([markdownContent.value], { type: 'text/markdown' })
  const url = URL.createObjectURL(blob)
  const a = document.createElement('a')
  a.href = url
  a.download = `changelog-${store.currentReport.releaseBranch.replace('/', '-')}.md`
  document.body.appendChild(a)
  a.click()
  document.body.removeChild(a)
  URL.revokeObjectURL(url)
  
  toast('Downloaded!', {
    description: 'Markdown file saved to your downloads'
  })
}

const downloadHtml = () => {
  if (!store.currentReport) return
  
  const fullHtml = generateHtmlReport(store.currentReport)
  const blob = new Blob([fullHtml], { type: 'text/html' })
  const url = URL.createObjectURL(blob)
  const a = document.createElement('a')
  a.href = url
  a.download = `changelog-${store.currentReport.releaseBranch.replace('/', '-')}.html`
  document.body.appendChild(a)
  a.click()
  document.body.removeChild(a)
  URL.revokeObjectURL(url)
  
  toast('Downloaded!', {
    description: 'HTML file saved to your downloads'
  })
}

// Computed properties
const wordCount = computed(() => {
  if (!markdownContent.value) return 0
  return markdownContent.value.split(/\s+/).filter(word => word.length > 0).length
})

const hasContent = computed(() => {
  return markdownContent.value.length > 0
})
</script>

<template>
  <Card>
    <CardHeader>
      <div class="flex items-center justify-between">
        <div>
          <CardTitle class="text-lg font-semibold">Markdown Preview</CardTitle>
          <CardDescription class="mt-1">
            Formatted changelog ready for copying to email or documentation
          </CardDescription>
        </div>
        <div class="flex items-center gap-2">
          <Badge v-if="hasContent" variant="secondary" class="px-3">
            {{ wordCount }} words
          </Badge>
        </div>
      </div>
      
      <!-- Action Buttons -->
      <div v-if="hasContent" class="flex flex-wrap items-center gap-2 pt-2">
        <!-- Primary Copy Button -->
        <Button 
          @click="copyHtml"
          :disabled="isCopyLoading"
          size="sm"
          class="h-8 px-8 w-48"
        >
          <CheckIcon v-if="copySuccess === 'html'" class="mr-2 h-4 w-4 text-green-600" />
          <CopyIcon v-else class="mr-2 h-4 w-4" />
          Copy
        </Button>
        
        <!-- Secondary Buttons -->
        <Button 
          @click="copyMarkdown"
          :disabled="isCopyLoading"
          variant="outline"
          size="sm"
          class="h-8"
        >
          <CheckIcon v-if="copySuccess === 'markdown'" class="mr-2 h-4 w-4 text-green-600" />
          <CopyIcon v-else class="mr-2 h-4 w-4" />
          Copy Markdown
        </Button>
        
        <Button 
          @click="downloadMarkdown"
          variant="outline"
          size="sm"
          class="h-8"
        >
          <DownloadIcon class="mr-2 h-4 w-4" />
          Download .md
        </Button>
        
        <Button 
          @click="downloadHtml"
          variant="outline"
          size="sm"
          class="h-8"
        >
          <DownloadIcon class="mr-2 h-4 w-4" />
          Download .html
        </Button>
      </div>
    </CardHeader>
    
    <CardContent>
      <!-- No Content State -->
      <div v-if="!store.currentReport && !isGenerating" class="text-center py-12 text-muted-foreground">
        <div class="text-lg font-medium mb-2">No preview available</div>
        <p>Generate a report to see the markdown preview here.</p>
      </div>
      
      <!-- Loading State -->
      <div v-else-if="isGenerating" class="text-center py-12 text-muted-foreground">
        <div class="flex items-center justify-center mb-4">
          <LoaderIcon class="h-6 w-6 animate-spin" />
        </div>
        <div class="text-lg font-medium mb-2">Generating preview...</div>
        <p>Converting report data to markdown format.</p>
      </div>
      
      <!-- Content -->
      <div v-else-if="hasContent" class="space-y-4">
        <!-- Usage Tips -->
        <div class="p-4 rounded-lg border border-border bg-muted/30">
          <h4 class="font-medium text-sm text-foreground mb-2">💡 Usage Tips</h4>
          <ul class="text-sm text-muted-foreground space-y-1">
            <li>• <strong>Copy</strong> (HTML format) for rich formatting in Outlook, Word, or Confluence</li>
            <li>• <strong>Copy Markdown</strong> for GitHub, GitLab, or other markdown-enabled platforms</li>
            <li>• <strong>Download</strong> files to save locally or attach to emails</li>
          </ul>
        </div>
        
        <!-- Preview Content -->
        <div class="prose prose-sm max-w-none">
          <div 
            class="markdown-content border rounded-lg p-6 bg-background"
            v-html="htmlContent"
          />
        </div>
      </div>
    </CardContent>
  </Card>
</template>

