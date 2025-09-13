<script lang="ts" setup>
import type { ToasterProps } from "vue-sonner"
import { Toaster as Sonner } from "vue-sonner"

const props = defineProps<ToasterProps>()
</script>

<template>
  <Sonner
    class="toaster group"
    v-bind="props"
    :style="{
      '--normal-bg': 'var(--popover)',
      '--normal-border': 'var(--border)',
      '--normal-text': 'var(--popover-foreground)',
      '--remove-button-size': '16px',
      '--remove-button-margin-right': '6px',
      '--loading-bar-bg': 'var(--muted)',
      '--success-bg': 'var(--popover)',
      '--success-border': 'var(--border)',
      '--success-text': 'var(--popover-foreground)',
      '--error-bg': 'var(--destructive)',
      '--error-border': 'var(--destructive)',
      '--error-text': 'var(--destructive-foreground)',
      '--warning-bg': 'var(--popover)',
      '--warning-border': 'var(--border)',
      '--warning-text': 'var(--popover-foreground)'
    }"
  />
</template>
