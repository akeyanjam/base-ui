<script setup lang="ts">
import { RouterView } from 'vue-router'
import { Toaster } from '@/components/ui/sonner'
</script>

<template>
  <div id="app">
    <Toaster />
    <RouterView />
  </div>
</template>


