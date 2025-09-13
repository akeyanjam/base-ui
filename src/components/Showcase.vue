<script setup lang="ts">
import type { DateValue } from "@internationalized/date"
import type { DateRange } from "reka-ui"
import type { Ref } from "vue"
import { DateFormatter, getLocalTimeZone, today } from "@internationalized/date"
import { ref } from "vue"
import { cn } from "@/lib/utils"
import Input from "./ui/input/Input.vue"
import Table from "./ui/table/Table.vue"
import TableHeader from "./ui/table/TableHeader.vue"
import TableBody from "./ui/table/TableBody.vue"
import TableRow from "./ui/table/TableRow.vue"
import TableCell from "./ui/table/TableCell.vue"
import { Button } from "./ui/button"
import { Calendar } from "./ui/calendar"
import { Popover, PopoverContent, PopoverTrigger } from "./ui/popover"
import { RangeCalendar } from "./ui/range-calendar"
import { Tabs, TabsContent, TabsList, TabsTrigger } from "./ui/tabs"
import { Card, CardContent, CardDescription, CardHeader } from "./ui/card"
import { Badge } from "./ui/badge"
import { Separator } from "./ui/separator"
import { CalendarIcon, TrendingUpIcon, UsersIcon, DollarSignIcon, CreditCardIcon } from "lucide-vue-next"

const inputValue = ref("")
const searchValue = ref("")
const df = new DateFormatter("en-US", { dateStyle: "medium" })
const value = ref<DateValue>()

const start = today(getLocalTimeZone())
const end = start.add({ days: 7 })
const rangeValue = ref({ start, end }) as Ref<DateRange>

const accountsData = [
  { account: "Checking Account", number: "****1234", balance: "$12,456.78", status: "Active" },
  { account: "Savings Account", number: "****5678", balance: "$45,123.90", status: "Active" },
  { account: "Credit Card", number: "****9012", balance: "-$1,234.56", status: "Current" },
  { account: "Investment Account", number: "****3456", balance: "$89,432.12", status: "Active" },
]

const transactionData = [
  { date: "2024-01-15", description: "Direct Deposit - Salary", amount: "+$4,250.00", category: "Income" },
  { date: "2024-01-14", description: "Mortgage Payment", amount: "-$1,850.00", category: "Housing" },
  { date: "2024-01-13", description: "Grocery Store", amount: "-$127.43", category: "Food" },
  { date: "2024-01-12", description: "Gas Station", amount: "-$65.20", category: "Transportation" },
  { date: "2024-01-11", description: "Online Transfer", amount: "-$500.00", category: "Transfer" },
]
</script>

<template>
  <div class="min-h-screen bg-background">
    <!-- Header Section -->
    <div class="bg-primary text-primary-foreground">
      <div class="container mx-auto px-6 py-12">
        <div class="max-w-4xl">
          <h1 class="text-4xl font-bold mb-4">Bank of America</h1>
          <h2 class="text-2xl font-light mb-6">Component Design System</h2>
          <p class="text-lg opacity-90 mb-8 leading-relaxed">
            A comprehensive showcase of our design system built with shadcn-vue, 
            featuring modern banking interface components with proper typography, 
            intuitive interactions, and consistent branding.
          </p>
        </div>
      </div>
    </div>

    <!-- Main Content -->
    <div class="container mx-auto px-6 py-12">
      <Tabs default-value="dashboard" class="space-y-8">
        <TabsList class="grid w-full grid-cols-4 max-w-2xl">
          <TabsTrigger value="dashboard" class="flex items-center gap-2">
            <TrendingUpIcon class="w-4 h-4" />
            Dashboard
          </TabsTrigger>
          <TabsTrigger value="accounts" class="flex items-center gap-2">
            <CreditCardIcon class="w-4 h-4" />
            Accounts
          </TabsTrigger>
          <TabsTrigger value="transactions" class="flex items-center gap-2">
            <DollarSignIcon class="w-4 h-4" />
            Transactions
          </TabsTrigger>
          <TabsTrigger value="components" class="flex items-center gap-2">
            <UsersIcon class="w-4 h-4" />
            Components
          </TabsTrigger>
        </TabsList>

        <!-- Dashboard Tab -->
        <TabsContent value="dashboard" class="space-y-8">
          <div class="grid grid-cols-1 md:grid-cols-3 gap-6">
            <Card>
              <CardHeader class="flex flex-row items-center justify-between space-y-0 pb-2">
                <div class="text-sm font-medium">Total Balance</div>
                <DollarSignIcon class="h-4 w-4 text-muted-foreground" />
              </CardHeader>
              <CardContent>
                <div class="text-2xl font-bold text-primary">$145,678.24</div>
                <p class="text-xs text-muted-foreground">+2.1% from last month</p>
              </CardContent>
            </Card>

            <Card>
              <CardHeader class="flex flex-row items-center justify-between space-y-0 pb-2">
                <div class="text-sm font-medium">Available Credit</div>
                <CreditCardIcon class="h-4 w-4 text-muted-foreground" />
              </CardHeader>
              <CardContent>
                <div class="text-2xl font-bold text-primary">$8,765.44</div>
                <p class="text-xs text-muted-foreground">87% available</p>
              </CardContent>
            </Card>

            <Card>
              <CardHeader class="flex flex-row items-center justify-between space-y-0 pb-2">
                <div class="text-sm font-medium">Monthly Spending</div>
                <TrendingUpIcon class="h-4 w-4 text-muted-foreground" />
              </CardHeader>
              <CardContent>
                <div class="text-2xl font-bold text-primary">$3,247.82</div>
                <p class="text-xs text-muted-foreground">-5.2% from last month</p>
              </CardContent>
            </Card>
          </div>

          <!-- Date Range Picker in Popover -->
          <Card>
            <CardHeader>
              <h3 class="text-lg font-semibold">Transaction Date Range</h3>
              <CardDescription>Select a date range to filter your transaction history</CardDescription>
            </CardHeader>
            <CardContent class="space-y-4">
              <Popover>
                <PopoverTrigger as-child>
                  <Button
                    variant="outline"
                    :class="cn('w-[300px] justify-start text-left font-normal', !rangeValue && 'text-muted-foreground')"
                  >
                    <CalendarIcon class="mr-2 h-4 w-4" />
                    <span v-if="rangeValue?.start && rangeValue?.end">
                      {{ df.format(rangeValue.start.toDate(getLocalTimeZone())) }} - {{ df.format(rangeValue.end.toDate(getLocalTimeZone())) }}
                    </span>
                    <span v-else>Pick a date range</span>
                  </Button>
                </PopoverTrigger>
                <PopoverContent class="w-auto p-0" align="start">
                  <RangeCalendar v-model="rangeValue" initial-focus />
                </PopoverContent>
              </Popover>
              <div class="flex gap-4">
                <Button @click="rangeValue = { start: today(getLocalTimeZone()).subtract({ days: 7 }), end: today(getLocalTimeZone()) }">
                  Last 7 Days
                </Button>
                <Button variant="outline" @click="rangeValue = { start: today(getLocalTimeZone()).subtract({ days: 30 }), end: today(getLocalTimeZone()) }">
                  Last 30 Days
                </Button>
              </div>
            </CardContent>
          </Card>
        </TabsContent>

        <!-- Accounts Tab -->
        <TabsContent value="accounts" class="space-y-6">
          <Card>
            <CardHeader>
              <h3 class="text-xl font-semibold">Account Summary</h3>
              <CardDescription>Overview of your Bank of America accounts</CardDescription>
            </CardHeader>
            <CardContent>
              <Table>
                <TableHeader>
                  <TableRow>
                    <TableCell class="font-semibold">Account Type</TableCell>
                    <TableCell class="font-semibold">Account Number</TableCell>
                    <TableCell class="font-semibold text-right">Balance</TableCell>
                    <TableCell class="font-semibold">Status</TableCell>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  <TableRow v-for="account in accountsData" :key="account.number">
                    <TableCell class="font-medium">{{ account.account }}</TableCell>
                    <TableCell class="text-muted-foreground">{{ account.number }}</TableCell>
                    <TableCell class="text-right font-mono" :class="account.balance.includes('-') ? 'text-destructive' : 'text-primary'">
                      {{ account.balance }}
                    </TableCell>
                    <TableCell>
                      <Badge variant="secondary">{{ account.status }}</Badge>
                    </TableCell>
                  </TableRow>
                </TableBody>
              </Table>
            </CardContent>
          </Card>
        </TabsContent>

        <!-- Transactions Tab -->
        <TabsContent value="transactions" class="space-y-6">
          <Card>
            <CardHeader>
              <h3 class="text-xl font-semibold">Recent Transactions</h3>
              <CardDescription>Your latest financial activity</CardDescription>
            </CardHeader>
            <CardContent>
              <div class="space-y-4 mb-6">
                <Input 
                  v-model="searchValue" 
                  placeholder="Search transactions..." 
                  class="max-w-sm" 
                />
              </div>
              <Table>
                <TableHeader>
                  <TableRow>
                    <TableCell class="font-semibold">Date</TableCell>
                    <TableCell class="font-semibold">Description</TableCell>
                    <TableCell class="font-semibold text-right">Amount</TableCell>
                    <TableCell class="font-semibold">Category</TableCell>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  <TableRow v-for="transaction in transactionData" :key="transaction.date + transaction.description">
                    <TableCell class="text-muted-foreground">{{ transaction.date }}</TableCell>
                    <TableCell class="font-medium">{{ transaction.description }}</TableCell>
                    <TableCell class="text-right font-mono" :class="transaction.amount.includes('+') ? 'text-green-600' : 'text-primary'">
                      {{ transaction.amount }}
                    </TableCell>
                    <TableCell>
                      <Badge variant="outline">{{ transaction.category }}</Badge>
                    </TableCell>
                  </TableRow>
                </TableBody>
              </Table>
            </CardContent>
          </Card>
        </TabsContent>

        <!-- Components Tab -->
        <TabsContent value="components" class="space-y-6">
          <div class="grid grid-cols-1 md:grid-cols-2 gap-6">
            <!-- Typography Card -->
            <Card>
              <CardHeader>
                <h3 class="text-lg font-semibold">Typography</h3>
                <CardDescription>Our typographic hierarchy</CardDescription>
              </CardHeader>
              <CardContent class="space-y-4">
                <div>
                  <h1 class="text-4xl font-bold text-primary mb-2">Heading 1</h1>
                  <h2 class="text-3xl font-bold text-primary mb-2">Heading 2</h2>
                  <h3 class="text-2xl font-semibold text-primary mb-2">Heading 3</h3>
                  <h4 class="text-xl font-semibold text-foreground mb-2">Heading 4</h4>
                  <p class="text-base text-foreground mb-2">Regular paragraph text with proper line height and spacing.</p>
                  <p class="text-sm text-muted-foreground">Small text for secondary information.</p>
                </div>
              </CardContent>
            </Card>

            <!-- Form Components Card -->
            <Card>
              <CardHeader>
                <h3 class="text-lg font-semibold">Form Components</h3>
                <CardDescription>Input fields and controls</CardDescription>
              </CardHeader>
              <CardContent class="space-y-4">
                <div>
                  <label class="text-sm font-medium text-foreground mb-2 block">Account Search</label>
                  <Input v-model="inputValue" placeholder="Search accounts..." />
                </div>
                <div>
                  <label class="text-sm font-medium text-foreground mb-2 block">Date Selection</label>
                  <Popover>
                    <PopoverTrigger as-child>
                      <Button
                        variant="outline"
                        :class="cn('w-full justify-start text-left font-normal', !value && 'text-muted-foreground')"
                      >
                        <CalendarIcon class="mr-2 h-4 w-4" />
                        {{ value ? df.format(value.toDate(getLocalTimeZone())) : "Pick a date" }}
                      </Button>
                    </PopoverTrigger>
                    <PopoverContent class="w-auto p-0">
                      <Calendar v-model="value" initial-focus />
                    </PopoverContent>
                  </Popover>
                </div>
              </CardContent>
            </Card>

            <!-- Buttons Card -->
            <Card>
              <CardHeader>
                <h3 class="text-lg font-semibold">Button Variants</h3>
                <CardDescription>Different button styles and states</CardDescription>
              </CardHeader>
              <CardContent class="space-y-4">
                <div class="flex flex-wrap gap-2">
                  <Button>Primary</Button>
                  <Button variant="secondary">Secondary</Button>
                  <Button variant="outline">Outline</Button>
                  <Button variant="ghost">Ghost</Button>
                  <Button variant="destructive">Destructive</Button>
                </div>
                <Separator />
                <div class="flex flex-wrap gap-2">
                  <Button size="sm">Small</Button>
                  <Button>Default</Button>
                  <Button size="lg">Large</Button>
                </div>
              </CardContent>
            </Card>

            <!-- Status Badges Card -->
            <Card>
              <CardHeader>
                <h3 class="text-lg font-semibold">Status Badges</h3>
                <CardDescription>Visual indicators for different states</CardDescription>
              </CardHeader>
              <CardContent class="space-y-4">
                <div class="flex flex-wrap gap-2">
                  <Badge>Default</Badge>
                  <Badge variant="secondary">Secondary</Badge>
                  <Badge variant="outline">Outline</Badge>
                  <Badge variant="destructive">Destructive</Badge>
                </div>
                <div class="space-y-2">
                  <div class="flex items-center justify-between">
                    <span class="text-sm">Account Status</span>
                    <Badge variant="secondary">Active</Badge>
                  </div>
                  <div class="flex items-center justify-between">
                    <span class="text-sm">Payment Status</span>
                    <Badge>Processed</Badge>
                  </div>
                  <div class="flex items-center justify-between">
                    <span class="text-sm">Alert Level</span>
                    <Badge variant="destructive">High</Badge>
                  </div>
                </div>
              </CardContent>
            </Card>
          </div>
        </TabsContent>
      </Tabs>
    </div>
  </div>
</template>

