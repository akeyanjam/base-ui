<script setup lang="ts">
import { computed } from 'vue'
import { useAppStore } from '@/stores/appStore'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Button } from '@/components/ui/button'
import { Badge } from '@/components/ui/badge'
import { Separator } from '@/components/ui/separator'
import type { RepoRef } from '@/types'

const store = useAppStore()

// Group repositories by project
const reposByProject = computed(() => {
  const groups: Record<string, RepoRef[]> = {}
  
  store.availableRepos.forEach(repo => {
    if (!groups[repo.projectKey]) {
      groups[repo.projectKey] = []
    }
    groups[repo.projectKey].push(repo)
  })
  
  // Sort projects and repos within each project
  const sortedGroups: Record<string, RepoRef[]> = {}
  Object.keys(groups).sort().forEach(projectKey => {
    sortedGroups[projectKey] = groups[projectKey].sort((a, b) => a.slug.localeCompare(b.slug))
  })
  
  return sortedGroups
})

// Check if repo is selected
const isRepoSelected = (repo: RepoRef) => {
  return store.selectedRepos.some(
    selected => selected.projectKey === repo.projectKey && selected.slug === repo.slug
  )
}

// Check if all repos in a project are selected
const isProjectFullySelected = (projectKey: string) => {
  const projectRepos = reposByProject.value[projectKey] || []
  return projectRepos.length > 0 && projectRepos.every(repo => isRepoSelected(repo))
}

// Check if some (but not all) repos in a project are selected
const isProjectPartiallySelected = (projectKey: string) => {
  const projectRepos = reposByProject.value[projectKey] || []
  const selectedCount = projectRepos.filter(repo => isRepoSelected(repo)).length
  return selectedCount > 0 && selectedCount < projectRepos.length
}

// Toggle all repos in a project
const toggleProject = (projectKey: string) => {
  const projectRepos = reposByProject.value[projectKey] || []
  const isFullySelected = isProjectFullySelected(projectKey)
  
  if (isFullySelected) {
    // Deselect all repos in project
    projectRepos.forEach(repo => {
      if (isRepoSelected(repo)) {
        store.toggleRepoSelection(repo)
      }
    })
  } else {
    // Select all repos in project
    projectRepos.forEach(repo => {
      if (!isRepoSelected(repo)) {
        store.toggleRepoSelection(repo)
      }
    })
  }
}

// Select all repositories
const selectAll = () => {
  store.selectAllRepos()
}

// Clear all selections
const clearAll = () => {
  store.clearRepoSelection()
}

// Check if all repos are selected
const allReposSelected = computed(() => {
  return store.availableRepos.length > 0 && store.selectedRepos.length === store.availableRepos.length
})

// Get project selection state for styling
const getProjectCheckboxState = (projectKey: string) => {
  if (isProjectFullySelected(projectKey)) return 'checked'
  if (isProjectPartiallySelected(projectKey)) return 'indeterminate'
  return 'unchecked'
}
</script>

<template>
  <Card>
    <CardHeader>
      <div class="flex items-center justify-between">
        <div>
          <CardTitle class="text-lg font-semibold">Repository Selection</CardTitle>
          <CardDescription class="mt-1">
            Choose repositories to include in your changelog report
          </CardDescription>
        </div>
        <div class="flex items-center gap-2">
          <Badge variant="secondary" class="px-3">
            {{ store.selectedRepoCount }} selected
          </Badge>
        </div>
      </div>
      
      <div class="flex items-center gap-2 pt-2">
        <Button 
          variant="outline" 
          size="sm" 
          @click="selectAll"
          :disabled="allReposSelected"
          class="h-8"
        >
          Select All
        </Button>
        <Button 
          variant="outline" 
          size="sm" 
          @click="clearAll"
          :disabled="store.selectedRepoCount === 0"
          class="h-8"
        >
          Clear All
        </Button>
      </div>
    </CardHeader>
    
    <CardContent>
      <div v-if="store.availableRepos.length === 0" class="text-center py-8 text-muted-foreground">
        <div class="text-lg font-medium mb-2">No repositories available</div>
        <p>Repository data is loading or unavailable.</p>
      </div>
      
      <div v-else class="space-y-6">
        <div 
          v-for="(repos, projectKey) in reposByProject" 
          :key="projectKey"
          class="space-y-3"
        >
          <!-- Project Header -->
          <div class="flex items-center gap-3">
            <label 
              class="flex items-center gap-3 cursor-pointer group hover:bg-accent/50 p-2 -m-2 rounded-md transition-colors"
              @click="toggleProject(projectKey)"
            >
              <div class="relative">
                <input
                  type="checkbox"
                  :checked="isProjectFullySelected(projectKey)"
                  :class="[
                    'w-4 h-4 rounded border-2 transition-all cursor-pointer',
                    getProjectCheckboxState(projectKey) === 'checked' 
                      ? 'bg-primary border-primary text-primary-foreground' 
                      : 'border-input bg-background',
                    getProjectCheckboxState(projectKey) === 'indeterminate'
                      ? 'bg-primary/50 border-primary'
                      : ''
                  ]"
                  @change.stop="toggleProject(projectKey)"
                />
                <!-- Indeterminate indicator -->
                <div 
                  v-if="getProjectCheckboxState(projectKey) === 'indeterminate'"
                  class="absolute inset-0 flex items-center justify-center pointer-events-none"
                >
                  <div class="w-2 h-0.5 bg-primary-foreground rounded-full"></div>
                </div>
              </div>
              
              <div class="flex items-center gap-2">
                <h3 class="font-semibold text-foreground group-hover:text-primary transition-colors">
                  {{ projectKey }}
                </h3>
                <Badge variant="outline" class="text-xs">
                  {{ repos.length }} {{ repos.length === 1 ? 'repo' : 'repos' }}
                </Badge>
              </div>
            </label>
          </div>
          
          <!-- Repository List -->
          <div class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-2 ml-7">
            <label 
              v-for="repo in repos" 
              :key="`${repo.projectKey}/${repo.slug}`"
              class="flex items-center gap-3 p-3 rounded-md border border-border cursor-pointer hover:border-primary/50 hover:bg-accent/30 transition-all group"
            >
              <input
                type="checkbox"
                :checked="isRepoSelected(repo)"
                class="w-4 h-4 rounded border-2 border-input bg-background text-primary focus:ring-2 focus:ring-ring focus:ring-offset-2 cursor-pointer transition-all"
                @change="store.toggleRepoSelection(repo)"
              />
              
              <div class="flex-1 min-w-0">
                <div class="font-medium text-sm text-foreground group-hover:text-primary transition-colors">
                  {{ repo.slug }}
                </div>
                <div class="text-xs text-muted-foreground font-mono">
                  {{ repo.projectKey }}/{{ repo.slug }}
                </div>
              </div>
            </label>
          </div>
          
          <Separator v-if="Object.keys(reposByProject).indexOf(projectKey) < Object.keys(reposByProject).length - 1" />
        </div>
      </div>
      
      <!-- Validation Error -->
      <div v-if="store.errors.repos" class="mt-4 p-3 bg-destructive/10 border border-destructive/20 rounded-md">
        <p class="text-sm text-destructive font-medium">{{ store.errors.repos }}</p>
      </div>
    </CardContent>
  </Card>
</template>
