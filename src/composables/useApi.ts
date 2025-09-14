import type { BuildReportRequest, BuildReportResponse, RepoMetadataResponse } from '@/types'

export function useApi() {
  const baseUrl = '/api' // Will be configured in production
  
  /**
   * Fetch available repositories and default release branch
   */
  const fetchRepos = async (): Promise<RepoMetadataResponse> => {
    try {
      const response = await fetch(`${baseUrl}/meta/repos`)
      if (!response.ok) {
        throw new Error(`Failed to fetch repositories: ${response.statusText}`)
      }
      return await response.json()
      
      // Development: Load from public folder with simulated delay
      // await new Promise(resolve => setTimeout(resolve, 800))
      // const response = await fetch('/mock-data.json')
      // if (!response.ok) {
      //   throw new Error(`Failed to fetch mock data: ${response.statusText}`)
      // }
      // 
      // const mockData = await response.json()
      // return {
      //   repos: mockData.repos,
      //   defaultReleaseBranch: mockData.defaultReleaseBranch
      // }
    } catch (error) {
      console.error('Error fetching repositories:', error)
      throw new Error(`Failed to load repositories: ${error instanceof Error ? error.message : 'Unknown error'}`)
    }
  }
  
  /**
   * Generate changelog report
   */
  const generateReport = async (request: BuildReportRequest): Promise<BuildReportResponse> => {
    try {
      const response = await fetch(`${baseUrl}/report/build`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify(request)
      })
      
      if (!response.ok) {
        const errorData = await response.json()
        throw new Error(errorData.detail || `API Error: ${response.statusText}`)
      }
      
      return await response.json()
      
      // Development: Simulate report generation with realistic delay
      // console.log('Generating report for:', request)
      // 
      // // Simulate processing steps with different delays
      // await new Promise(resolve => setTimeout(resolve, 1000)) // Initial processing
      // await new Promise(resolve => setTimeout(resolve, 1500)) // Bitbucket queries
      // await new Promise(resolve => setTimeout(resolve, 800))  // Jira enrichment
      // await new Promise(resolve => setTimeout(resolve, 600))  // Report assembly
      // 
      // // Load mock report
      // const response = await fetch('/mock-data.json')
      // if (!response.ok) {
      //   throw new Error(`Failed to fetch mock data: ${response.statusText}`)
      // }
      // 
      // const mockData = await response.json()
      // const mockReport = mockData.sampleReport
      // 
      // // Customize mock report with request parameters
      // const customizedReport: BuildReportResponse = {
      //   ...mockReport,
      //   releaseBranch: request.releaseBranch,
      //   from: request.from,
      //   to: request.to,
      //   generatedAt: new Date().toISOString(),
      //   // Filter stories based on selected repos (simulate realistic filtering)
      //   stories: mockReport.stories.filter((story: any) => 
      //     request.repos.some(repo => 
      //       repo.projectKey === story.repo.projectKey && 
      //       repo.slug === story.repo.slug
      //     )
      //   )
      // }
      // 
      // // Recalculate summary based on filtered stories
      // const repoBreakdown: Record<string, number> = {}
      // const statusBreakdown: Record<string, number> = {}
      // 
      // customizedReport.stories.forEach(story => {
      //   const repoKey = `${story.repo.projectKey}/${story.repo.slug}`
      //   repoBreakdown[repoKey] = (repoBreakdown[repoKey] || 0) + 1
      //   statusBreakdown[story.issue.status] = (statusBreakdown[story.issue.status] || 0) + 1
      // })
      // 
      // customizedReport.summary = {
      //   totalStories: customizedReport.stories.length,
      //   repoBreakdown,
      //   statusBreakdown
      // }
      // 
      // return customizedReport
      
    } catch (error) {
      console.error('Error generating report:', error)
      throw new Error(`Failed to generate report: ${error instanceof Error ? error.message : 'Unknown error'}`)
    }
  }
  
  /**
   * Health check endpoint
   */
  const healthCheck = async (): Promise<{ status: string; timestamp: string; version: string }> => {
    try {
      const response = await fetch(`${baseUrl}/health`)
      if (!response.ok) {
        throw new Error(`Health check failed: ${response.statusText}`)
      }
      return await response.json()
      
      // Development: Mock health check
      // await new Promise(resolve => setTimeout(resolve, 200))
      // return {
      //   status: 'Healthy',
      //   timestamp: new Date().toISOString(),
      //   version: '1.0.0-dev'
      // }
    } catch (error) {
      console.error('Health check failed:', error)
      throw new Error(`Health check failed: ${error instanceof Error ? error.message : 'Unknown error'}`)
    }
  }
  
  return {
    fetchRepos,
    generateReport,
    healthCheck
  }
}
