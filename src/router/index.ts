import { createRouter, createWebHistory } from 'vue-router'

const router = createRouter({
  history: createWebHistory(),
  routes: [
    {
      path: '/',
      name: 'Criteria',
      component: () => import('../views/ReportCriteria.vue'),
      meta: {
        title: 'SIT Changelog Builder - Configure Report'
      }
    },
    {
      path: '/results',
      name: 'Results', 
      component: () => import('../views/ReportResults.vue'),
      meta: {
        title: 'SIT Changelog Builder - Report Results'
      }
    },
    {
      path: '/showcase',
      name: 'Showcase',
      component: () => import('../components/Showcase.vue'),
      meta: {
        title: 'Bank of America - Component Showcase'
      }
    }
  ]
})

// Update document title on route change
router.beforeEach((to) => {
  document.title = to.meta?.title as string || 'SIT Changelog Builder'
})

export default router
