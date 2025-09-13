import { ref } from 'vue'

export function useClipboard() {
  const isSupported = ref(typeof navigator !== 'undefined' && 'clipboard' in navigator)
  const isLoading = ref(false)
  const error = ref<string | null>(null)
  
  /**
   * Copy text to clipboard using modern Clipboard API with fallback
   */
  const copyToClipboard = async (text: string): Promise<boolean> => {
    if (!text) {
      error.value = 'No content to copy'
      return false
    }
    
    isLoading.value = true
    error.value = null
    
    try {
      // Try modern Clipboard API first
      if (isSupported.value && navigator.clipboard?.writeText) {
        await navigator.clipboard.writeText(text)
        return true
      }
      
      // Fallback to legacy method
      return await legacyCopy(text)
      
    } catch (err) {
      error.value = `Failed to copy: ${err instanceof Error ? err.message : 'Unknown error'}`
      console.error('Clipboard error:', err)
      
      // Try legacy fallback on modern API failure
      try {
        return await legacyCopy(text)
      } catch (fallbackErr) {
        error.value = 'Copy operation not supported in this browser'
        return false
      }
    } finally {
      isLoading.value = false
    }
  }
  
  /**
   * Legacy clipboard copy using execCommand (for older browsers)
   */
  const legacyCopy = async (text: string): Promise<boolean> => {
    return new Promise((resolve) => {
      const textArea = document.createElement('textarea')
      textArea.value = text
      textArea.style.position = 'fixed'
      textArea.style.left = '-999999px'
      textArea.style.top = '-999999px'
      textArea.setAttribute('readonly', '')
      textArea.setAttribute('aria-hidden', 'true')
      
      document.body.appendChild(textArea)
      
      try {
        textArea.select()
        textArea.setSelectionRange(0, 99999) // For mobile devices
        
        const successful = document.execCommand('copy')
        document.body.removeChild(textArea)
        
        if (successful) {
          resolve(true)
        } else {
          throw new Error('execCommand copy failed')
        }
      } catch (err) {
        document.body.removeChild(textArea)
        throw err
      }
    })
  }
  
  /**
   * Copy HTML content to clipboard (preserves formatting)
   */
  const copyHtmlToClipboard = async (html: string, fallbackText?: string): Promise<boolean> => {
    if (!html) {
      error.value = 'No HTML content to copy'
      return false
    }
    
    isLoading.value = true
    error.value = null
    
    try {
      // Modern browsers support ClipboardItem for rich content
      if (navigator.clipboard?.write) {
        const clipboardItem = new ClipboardItem({
          'text/html': new Blob([html], { type: 'text/html' }),
          'text/plain': new Blob([fallbackText || stripHtml(html)], { type: 'text/plain' })
        })
        
        await navigator.clipboard.write([clipboardItem])
        return true
      }
      
      // Fallback to plain text
      return await copyToClipboard(fallbackText || stripHtml(html))
      
    } catch (err) {
      error.value = `Failed to copy HTML: ${err instanceof Error ? err.message : 'Unknown error'}`
      console.error('HTML clipboard error:', err)
      
      // Fallback to plain text
      return await copyToClipboard(fallbackText || stripHtml(html))
    } finally {
      isLoading.value = false
    }
  }
  
  /**
   * Strip HTML tags from string
   */
  const stripHtml = (html: string): string => {
    const temp = document.createElement('div')
    temp.innerHTML = html
    return temp.textContent || temp.innerText || ''
  }
  
  /**
   * Read text from clipboard
   */
  const readFromClipboard = async (): Promise<string | null> => {
    if (!isSupported.value || !navigator.clipboard?.readText) {
      error.value = 'Reading clipboard not supported'
      return null
    }
    
    isLoading.value = true
    error.value = null
    
    try {
      const text = await navigator.clipboard.readText()
      return text
    } catch (err) {
      error.value = `Failed to read clipboard: ${err instanceof Error ? err.message : 'Unknown error'}`
      console.error('Clipboard read error:', err)
      return null
    } finally {
      isLoading.value = false
    }
  }
  
  /**
   * Check if clipboard has content
   */
  const hasClipboardContent = async (): Promise<boolean> => {
    try {
      const text = await readFromClipboard()
      return text !== null && text.length > 0
    } catch {
      return false
    }
  }
  
  /**
   * Clear any clipboard error
   */
  const clearError = () => {
    error.value = null
  }
  
  return {
    isSupported,
    isLoading,
    error,
    copyToClipboard,
    copyHtmlToClipboard,
    readFromClipboard,
    hasClipboardContent,
    clearError
  }
}
