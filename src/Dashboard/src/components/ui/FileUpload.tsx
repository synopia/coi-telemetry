import { useState, useRef, forwardRef, type InputHTMLAttributes } from 'react'
import { Upload, X, File, FileText, Image as ImageIcon } from 'lucide-react'
import { cn } from '@/lib/utils'

export interface FileUploadProps extends Omit<InputHTMLAttributes<HTMLInputElement>, 'type'> {
  label?: string
  error?: string
  helperText?: string
  maxSize?: number // in bytes
  onFilesChange?: (files: File[]) => void
  showPreview?: boolean
  variant?: 'default' | 'compact'
}

const FileUpload = forwardRef<HTMLInputElement, FileUploadProps>(
  (
    {
      label,
      error,
      helperText,
      maxSize,
      onFilesChange,
      showPreview = true,
      variant = 'default',
      accept,
      multiple = false,
      className,
      disabled,
      ...props
    },
    ref
  ) => {
    const [files, setFiles] = useState<File[]>([])
    const [isDragging, setIsDragging] = useState(false)
    const inputRef = useRef<HTMLInputElement>(null)

    const handleFileChange = (newFiles: FileList | null) => {
      if (!newFiles) return

      const fileArray = Array.from(newFiles)

      // Filter files by max size if specified
      const validFiles = maxSize
        ? fileArray.filter((file) => file.size <= maxSize)
        : fileArray

      if (multiple) {
        setFiles((prev) => [...prev, ...validFiles])
        onFilesChange?.([...files, ...validFiles])
      } else {
        setFiles(validFiles)
        onFilesChange?.(validFiles)
      }
    }

    const handleDragEnter = (e: React.DragEvent) => {
      e.preventDefault()
      e.stopPropagation()
      if (!disabled) setIsDragging(true)
    }

    const handleDragLeave = (e: React.DragEvent) => {
      e.preventDefault()
      e.stopPropagation()
      setIsDragging(false)
    }

    const handleDragOver = (e: React.DragEvent) => {
      e.preventDefault()
      e.stopPropagation()
    }

    const handleDrop = (e: React.DragEvent) => {
      e.preventDefault()
      e.stopPropagation()
      setIsDragging(false)

      if (disabled) return

      const droppedFiles = e.dataTransfer.files
      handleFileChange(droppedFiles)
    }

    const removeFile = (index: number) => {
      const newFiles = files.filter((_, i) => i !== index)
      setFiles(newFiles)
      onFilesChange?.(newFiles)
    }

    const formatFileSize = (bytes: number) => {
      if (bytes === 0) return '0 Bytes'
      const k = 1024
      const sizes = ['Bytes', 'KB', 'MB', 'GB']
      const i = Math.floor(Math.log(bytes) / Math.log(k))
      return Math.round(bytes / Math.pow(k, i) * 100) / 100 + ' ' + sizes[i]
    }

    const getFileIcon = (file: File) => {
      if (file.type.startsWith('image/')) return <ImageIcon className="h-5 w-5" />
      if (file.type.startsWith('text/')) return <FileText className="h-5 w-5" />
      return <File className="h-5 w-5" />
    }

    const isImage = (file: File) => file.type.startsWith('image/')

    if (variant === 'compact') {
      return (
        <div className={cn('w-full', className)}>
          {label && (
            <label className="mb-2 block text-sm font-medium text-secondary-900 dark:text-white">
              {label}
              {props.required && <span className="ml-1 text-danger-500">*</span>}
            </label>
          )}
          <div className="flex items-center gap-2">
            <input
              ref={ref || inputRef}
              type="file"
              accept={accept}
              multiple={multiple}
              disabled={disabled}
              onChange={(e) => handleFileChange(e.target.files)}
              className="hidden"
              {...props}
            />
            <button
              type="button"
              onClick={() => inputRef.current?.click()}
              disabled={disabled}
              className={cn(
                'inline-flex items-center gap-2 px-4 py-2 rounded-lg border',
                'text-sm font-medium transition-colors',
                'focus:outline-none focus:ring-2 focus:ring-primary-500/20',
                error
                  ? 'border-danger-300 dark:border-danger-600 text-danger-600 dark:text-danger-400'
                  : 'border-secondary-300 dark:border-secondary-600 text-secondary-700 dark:text-secondary-300',
                'hover:bg-secondary-50 dark:hover:bg-secondary-800',
                disabled && 'cursor-not-allowed opacity-60'
              )}
            >
              <Upload className="h-4 w-4" />
              Choose File{multiple ? 's' : ''}
            </button>
            {files.length > 0 && (
              <span className="text-sm text-secondary-600 dark:text-secondary-400">
                {files.length} file{files.length > 1 ? 's' : ''} selected
              </span>
            )}
          </div>
          {(error || helperText) && (
            <p
              className={cn(
                'mt-1.5 text-sm',
                error
                  ? 'text-danger-600 dark:text-danger-400'
                  : 'text-secondary-600 dark:text-secondary-400'
              )}
            >
              {error || helperText}
            </p>
          )}
        </div>
      )
    }

    return (
      <div className={cn('w-full', className)}>
        {label && (
          <label className="mb-2 block text-sm font-medium text-secondary-900 dark:text-white">
            {label}
            {props.required && <span className="ml-1 text-danger-500">*</span>}
          </label>
        )}

        <div
          onDragEnter={handleDragEnter}
          onDragOver={handleDragOver}
          onDragLeave={handleDragLeave}
          onDrop={handleDrop}
          className={cn(
            'relative rounded-lg border-2 border-dashed p-8 transition-colors',
            isDragging
              ? 'border-primary-500 bg-primary-50 dark:bg-primary-900/10'
              : error
              ? 'border-danger-300 dark:border-danger-600'
              : 'border-secondary-300 dark:border-secondary-600',
            !disabled && 'cursor-pointer hover:border-secondary-400 dark:hover:border-secondary-500',
            disabled && 'cursor-not-allowed opacity-60'
          )}
          onClick={() => !disabled && inputRef.current?.click()}
        >
          <input
            ref={ref || inputRef}
            type="file"
            accept={accept}
            multiple={multiple}
            disabled={disabled}
            onChange={(e) => handleFileChange(e.target.files)}
            className="hidden"
            {...props}
          />

          <div className="flex flex-col items-center justify-center text-center">
            <Upload
              className={cn(
                'h-12 w-12 mb-4',
                isDragging
                  ? 'text-primary-600 dark:text-primary-400'
                  : 'text-secondary-400 dark:text-secondary-500'
              )}
            />
            <p className="text-sm font-medium text-secondary-900 dark:text-white mb-1">
              {isDragging ? 'Drop files here' : 'Click to upload or drag and drop'}
            </p>
            <p className="text-xs text-secondary-600 dark:text-secondary-400">
              {accept ? accept.split(',').join(', ') : 'Any file type'}
              {maxSize && ` (Max ${formatFileSize(maxSize)})`}
            </p>
          </div>
        </div>

        {(error || helperText) && (
          <p
            className={cn(
              'mt-1.5 text-sm',
              error
                ? 'text-danger-600 dark:text-danger-400'
                : 'text-secondary-600 dark:text-secondary-400'
            )}
          >
            {error || helperText}
          </p>
        )}

        {/* File Preview */}
        {showPreview && files.length > 0 && (
          <div className="mt-4 space-y-2">
            {files.map((file, index) => (
              <div
                key={index}
                className="flex items-center gap-3 rounded-lg border border-secondary-200 dark:border-secondary-700 p-3 bg-white dark:bg-secondary-800"
              >
                {isImage(file) && showPreview ? (
                  <img
                    src={URL.createObjectURL(file)}
                    alt={file.name}
                    className="h-10 w-10 rounded object-cover"
                  />
                ) : (
                  <div className="flex h-10 w-10 items-center justify-center rounded bg-secondary-100 dark:bg-secondary-700 text-secondary-600 dark:text-secondary-400">
                    {getFileIcon(file)}
                  </div>
                )}
                <div className="flex-1 min-w-0">
                  <p className="text-sm font-medium text-secondary-900 dark:text-white truncate">
                    {file.name}
                  </p>
                  <p className="text-xs text-secondary-600 dark:text-secondary-400">
                    {formatFileSize(file.size)}
                  </p>
                </div>
                <button
                  type="button"
                  onClick={(e) => {
                    e.stopPropagation()
                    removeFile(index)
                  }}
                  className="flex-shrink-0 rounded p-1 text-secondary-400 hover:bg-secondary-100 dark:hover:bg-secondary-700 hover:text-secondary-600 dark:hover:text-secondary-300 transition-colors"
                >
                  <X className="h-4 w-4" />
                </button>
              </div>
            ))}
          </div>
        )}
      </div>
    )
  }
)

FileUpload.displayName = 'FileUpload'

export default FileUpload
