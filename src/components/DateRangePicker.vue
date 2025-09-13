<script setup lang="ts">
import type { DateRange } from "reka-ui"
import type { Ref } from "vue"
import { DateFormatter, getLocalTimeZone, today } from "@internationalized/date"
import { computed, ref, watch } from "vue"
import { Button } from '@/components/ui/button'
import { RangeCalendar } from '@/components/ui/range-calendar'
import { Popover, PopoverContent, PopoverTrigger } from '@/components/ui/popover'
import { CalendarIcon } from 'lucide-vue-next'
import { cn } from '@/lib/utils'

// Props for initial value and change handler
interface Props {
  modelValue?: DateRange
  placeholder?: string
}

interface Emits {
  (e: 'update:modelValue', value: DateRange): void
}

const props = withDefaults(defineProps<Props>(), {
  placeholder: 'Pick a date range'
})

const emit = defineEmits<Emits>()

const df = new DateFormatter("en-US", { dateStyle: "medium" })

// Initialize with prop value or default
const initializeDateRange = (): DateRange => {
  if (props.modelValue?.start && props.modelValue?.end) {
    return props.modelValue
  }
  
  // Default to last 7 days
  const end = today(getLocalTimeZone())
  const start = end.subtract({ days: 7 })
  return { start, end }
}

const value = ref(initializeDateRange()) as Ref<DateRange>

// Watch for external changes
watch(() => props.modelValue, (newValue) => {
  if (newValue && newValue !== value.value) {
    value.value = newValue
  }
}, { deep: true })

// Emit changes
watch(value, (newRange) => {
  if (newRange?.start && newRange?.end) {
    emit('update:modelValue', newRange)
  }
}, { deep: true })

// Display formatted date range
const dateRangeDisplay = computed(() => {
  if (!value.value?.start || !value.value?.end) {
    return props.placeholder
  }
  
  try {
    const from = df.format(value.value.start.toDate(getLocalTimeZone()))
    const to = df.format(value.value.end.toDate(getLocalTimeZone()))
    return `${from} - ${to}`
  } catch {
    return props.placeholder
  }
})
</script>

<template>
  <Popover>
    <PopoverTrigger as-child>
      <Button
        variant="outline"
        :class="cn(
          'w-full justify-start text-left font-normal',
          !value?.start && 'text-muted-foreground'
        )"
      >
        <CalendarIcon class="mr-2 h-4 w-4" />
        {{ dateRangeDisplay }}
      </Button>
    </PopoverTrigger>
    <PopoverContent class="w-auto p-0" align="start">
      <RangeCalendar v-model="value" initial-focus />
    </PopoverContent>
  </Popover>
</template>