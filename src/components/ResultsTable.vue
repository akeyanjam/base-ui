<script setup lang="ts">
import { ref, computed } from 'vue'
import { useAppStore } from '@/stores/appStore'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Button } from '@/components/ui/button'
import { Badge } from '@/components/ui/badge'
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table'
import { 
  ChevronDownIcon, 
  ChevronRightIcon, 
  ExternalLinkIcon, 
  GitBranchIcon,
  CalendarIcon,
  UserIcon
} from 'lucide-vue-next'
import type { StoryInfo } from '@/types'

const store = useAppStore()

// Track expanded rows
const expandedRows = ref<Set<string>>(new Set())

// Toggle row expansion
const toggleRow = (storyKey: string) => {
  if (expandedRows.value.has(storyKey)) {
    expandedRows.value.delete(storyKey)
  } else {
    expandedRows.value.add(storyKey)
  }
}

// Check if row is expanded
const isRowExpanded = (storyKey: string) => {
  return expandedRows.value.has(storyKey)
}

// Expand all rows
const expandAll = () => {
  if (store.currentReport?.stories) {
    expandedRows.value = new Set(store.currentReport.stories.map(story => story.issue.key))
  }
}

// Collapse all rows
const collapseAll = () => {
  expandedRows.value.clear()
}

// Get stories sorted by repository and then by issue key
const sortedStories = computed(() => {
  if (!store.currentReport?.stories) return []
  
  return [...store.currentReport.stories].sort((a, b) => {
    // First sort by repo
    const repoA = `${a.repo.projectKey}/${a.repo.slug}`
    const repoB = `${b.repo.projectKey}/${b.repo.slug}`
    if (repoA !== repoB) {
      return repoA.localeCompare(repoB)
    }
    
    // Then sort by issue key
    return a.issue.key.localeCompare(b.issue.key)
  })
})

// Get status badge variant
const getStatusVariant = (status: string): "default" | "secondary" | "destructive" | "outline" => {
  switch (status.toLowerCase()) {
    case 'done':
      return 'default'
    case 'in qa':
    case 'qa':
      return 'secondary'
    case 'in progress':
    case 'progress':
      return 'outline'
    default:
      return 'outline'
  }
}

// Format date
const formatDate = (dateString: string) => {
  try {
    const date = new Date(dateString)
    return date.toLocaleDateString('en-US', {
      month: 'short',
      day: 'numeric',
      year: 'numeric',
      hour: '2-digit',
      minute: '2-digit'
    })
  } catch {
    return dateString
  }
}

// Get relationship badge variant
const getRelationshipVariant = (relationship: string): "default" | "secondary" | "destructive" | "outline" => {
  switch (relationship.toLowerCase()) {
    case 'epic link':
      return 'default'
    case 'required by':
      return 'secondary'
    case 'relates':
      return 'outline'
    default:
      return 'outline'
  }
}
</script>

<template>
  <Card>
    <CardHeader>
      <div class="flex items-center justify-between">
        <div>
          <CardTitle class="text-lg font-semibold">Report Results</CardTitle>
          <CardDescription class="mt-1">
            Stories discovered for {{ store.currentReport?.releaseBranch }}
          </CardDescription>
        </div>
        <div class="flex items-center gap-2">
          <Button variant="outline" size="sm" @click="expandAll" class="h-8">
            Expand All
          </Button>
          <Button variant="outline" size="sm" @click="collapseAll" class="h-8">
            Collapse All
          </Button>
        </div>
      </div>
    </CardHeader>
    
    <CardContent>
      <div v-if="!store.currentReport" class="text-center py-12 text-muted-foreground">
        <div class="text-lg font-medium mb-2">No results yet</div>
        <p>Generate a report to see the results here.</p>
      </div>
      
      <div v-else-if="sortedStories.length === 0" class="text-center py-12 text-muted-foreground">
        <div class="text-lg font-medium mb-2">No stories found</div>
        <p>No pull requests found for the selected criteria. Try adjusting your date range or repository selection.</p>
      </div>
      
      <div v-else>
        <!-- Summary Stats -->
        <div class="grid grid-cols-1 md:grid-cols-3 gap-4 mb-6 p-4 rounded-lg border border-border bg-muted/30">
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
            <div class="text-sm text-muted-foreground">Statuses</div>
          </div>
        </div>
        
        <!-- Stories Table -->
        <div class="border rounded-md">
          <Table>
            <TableHeader>
              <TableRow class="bg-muted/50">
                <TableHead class="w-10"></TableHead>
                <TableHead class="font-semibold">Issue</TableHead>
                <TableHead class="font-semibold">Repository</TableHead>
                <TableHead class="font-semibold">Status</TableHead>
                <TableHead class="font-semibold">Assignee</TableHead>
                <TableHead class="font-semibold text-right">PRs</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              <template v-for="story in sortedStories" :key="story.issue.key">
                <!-- Main Story Row -->
                <TableRow 
                  class="cursor-pointer hover:bg-muted/30 transition-colors"
                  @click="toggleRow(story.issue.key)"
                >
                  <TableCell class="w-10">
                    <Button variant="ghost" size="sm" class="h-6 w-6 p-0">
                      <ChevronDownIcon 
                        v-if="isRowExpanded(story.issue.key)"
                        class="h-4 w-4" 
                      />
                      <ChevronRightIcon 
                        v-else
                        class="h-4 w-4" 
                      />
                    </Button>
                  </TableCell>
                  
                  <TableCell>
                    <div class="flex items-center gap-2">
                      <a 
                        :href="story.issue.url" 
                        target="_blank"
                        class="font-mono text-sm text-primary hover:underline font-medium"
                        @click.stop
                      >
                        {{ story.issue.key }}
                        <ExternalLinkIcon class="inline h-3 w-3 ml-1" />
                      </a>
                    </div>
                    <div class="text-sm text-foreground font-medium mt-1 pr-4">
                      {{ story.issue.summary }}
                    </div>
                  </TableCell>
                  
                  <TableCell>
                    <div class="font-mono text-sm">
                      <span class="text-muted-foreground">{{ story.repo.projectKey }}/</span>
                      <span class="font-medium">{{ story.repo.slug }}</span>
                    </div>
                  </TableCell>
                  
                  <TableCell>
                    <Badge :variant="getStatusVariant(story.issue.status)">
                      {{ story.issue.status }}
                    </Badge>
                  </TableCell>
                  
                  <TableCell>
                    <div v-if="story.issue.assignee" class="flex items-center gap-1 text-sm">
                      <UserIcon class="h-3 w-3 text-muted-foreground" />
                      {{ story.issue.assignee }}
                    </div>
                    <span v-else class="text-muted-foreground text-sm">Unassigned</span>
                  </TableCell>
                  
                  <TableCell class="text-right">
                    <Badge variant="outline" class="text-xs">
                      {{ story.pullRequests.length }}
                    </Badge>
                  </TableCell>
                </TableRow>
                
                <!-- Expanded Details Row -->
                <TableRow v-if="isRowExpanded(story.issue.key)" class="bg-muted/20">
                  <TableCell colspan="6" class="py-6">
                    <div class="space-y-6 max-w-5xl">
                      
                      <!-- Related Epics -->
                      <div v-if="story.issue.relatedEpics.length > 0">
                        <h4 class="font-semibold text-sm text-foreground mb-3 flex items-center gap-2">
                          <GitBranchIcon class="h-4 w-4" />
                          Related Epics
                        </h4>
                        <div class="grid grid-cols-1 md:grid-cols-2 gap-3">
                          <div 
                            v-for="epic in story.issue.relatedEpics" 
                            :key="epic.key"
                            class="p-3 rounded-md border border-border bg-card"
                          >
                            <div class="flex items-start justify-between gap-2">
                              <div class="flex-1 min-w-0">
                                <div class="flex items-center gap-2">
                                  <a 
                                    v-if="epic.url"
                                    :href="epic.url" 
                                    target="_blank"
                                    class="font-mono text-sm text-primary hover:underline font-medium"
                                  >
                                    {{ epic.key }}
                                    <ExternalLinkIcon class="inline h-3 w-3 ml-1" />
                                  </a>
                                  <span v-else class="font-mono text-sm font-medium">{{ epic.key }}</span>
                                </div>
                                <div v-if="epic.summary" class="text-sm text-muted-foreground mt-1">
                                  {{ epic.summary }}
                                </div>
                              </div>
                              <Badge :variant="getRelationshipVariant(epic.relationship)" class="text-xs shrink-0">
                                {{ epic.relationship }}
                              </Badge>
                            </div>
                          </div>
                        </div>
                      </div>
                      
                      <!-- Pull Requests -->
                      <div v-if="story.pullRequests.length > 0">
                        <h4 class="font-semibold text-sm text-foreground mb-3 flex items-center gap-2">
                          <GitBranchIcon class="h-4 w-4" />
                          Pull Requests
                        </h4>
                        <div class="space-y-3">
                          <div 
                            v-for="pr in story.pullRequests" 
                            :key="pr.id"
                            class="p-4 rounded-md border border-border bg-card"
                          >
                            <div class="flex items-start justify-between gap-4">
                              <div class="flex-1 min-w-0">
                                <div class="flex items-center gap-2 mb-2">
                                  <a 
                                    :href="pr.url" 
                                    target="_blank"
                                    class="font-medium text-primary hover:underline"
                                  >
                                    PR #{{ pr.id }}
                                    <ExternalLinkIcon class="inline h-3 w-3 ml-1" />
                                  </a>
                                  <Badge variant="secondary" class="text-xs">Merged</Badge>
                                </div>
                                <div class="text-sm text-foreground mb-3">
                                  {{ pr.title }}
                                </div>
                                <div class="grid grid-cols-1 md:grid-cols-2 gap-4 text-xs text-muted-foreground">
                                  <div>
                                    <div class="flex items-center gap-1 mb-1">
                                      <GitBranchIcon class="h-3 w-3" />
                                      Branch
                                    </div>
                                    <div class="font-mono">
                                      <span class="text-green-600">{{ pr.sourceBranch }}</span>
                                      <span class="mx-2">→</span>
                                      <span class="text-blue-600">{{ pr.targetBranch }}</span>
                                    </div>
                                  </div>
                                  <div>
                                    <div class="flex items-center gap-1 mb-1">
                                      <CalendarIcon class="h-3 w-3" />
                                      Merged
                                    </div>
                                    <div>{{ formatDate(pr.mergedAt) }}</div>
                                    <div v-if="pr.mergedBy" class="flex items-center gap-1 mt-1">
                                      <UserIcon class="h-3 w-3" />
                                      {{ pr.mergedBy }}
                                    </div>
                                  </div>
                                </div>
                              </div>
                            </div>
                          </div>
                        </div>
                      </div>
                      
                      <!-- Source Branches (if different from PR branches) -->
                      <div v-if="story.sourceBranches.length > 0">
                        <h4 class="font-semibold text-sm text-foreground mb-3">Source Branches</h4>
                        <div class="flex flex-wrap gap-2">
                          <Badge 
                            v-for="branch in story.sourceBranches" 
                            :key="branch"
                            variant="outline" 
                            class="font-mono text-xs"
                          >
                            {{ branch }}
                          </Badge>
                        </div>
                      </div>
                      
                    </div>
                  </TableCell>
                </TableRow>
              </template>
            </TableBody>
          </Table>
        </div>
      </div>
    </CardContent>
  </Card>
</template>

