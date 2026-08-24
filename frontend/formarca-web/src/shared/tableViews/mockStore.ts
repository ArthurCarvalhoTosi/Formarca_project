import { ApiError } from '@/shared/api/types'
import { getMockSessionByToken } from '@/shared/auth/mockAuth'
import { tokenStorage } from '@/shared/auth/tokenStorage'
import {
  isTableKey,
  type CreateTableViewRequest,
  type TableViewDto,
  type UpdateTableViewRequest,
} from './types'

type StoredTableView = TableViewDto & { userId: string }

const views: StoredTableView[] = []

function requireUserId(): string {
  const session = getMockSessionByToken(tokenStorage.getAccessToken())
  if (!session?.user.id) {
    throw new ApiError(401, 'Unauthorized')
  }
  return session.user.id
}

function toDto(view: StoredTableView): TableViewDto {
  return {
    id: view.id,
    tableKey: view.tableKey,
    name: view.name,
    columnKeys: [...view.columnKeys],
    isActive: view.isActive,
  }
}

function assertTableKey(tableKey: string): void {
  if (!isTableKey(tableKey)) {
    throw new ApiError(400, 'Invalid tableKey')
  }
}

function assertColumnKeys(columnKeys: string[] | undefined): void {
  if (!columnKeys || columnKeys.length === 0) {
    throw new ApiError(400, 'columnKeys must not be empty')
  }
}

function nameTaken(
  userId: string,
  tableKey: string,
  name: string,
  exceptId?: string,
): boolean {
  const normalized = name.trim().toLowerCase()
  return views.some(
    (view) =>
      view.userId === userId &&
      view.tableKey === tableKey &&
      view.id !== exceptId &&
      view.name.trim().toLowerCase() === normalized,
  )
}

function deactivateOthers(userId: string, tableKey: string, exceptId: string): void {
  for (const view of views) {
    if (view.userId === userId && view.tableKey === tableKey && view.id !== exceptId) {
      view.isActive = false
    }
  }
}

function forUserTable(userId: string, tableKey: string): StoredTableView[] {
  return views
    .filter((view) => view.userId === userId && view.tableKey === tableKey)
    .sort((a, b) => a.name.localeCompare(b.name, undefined, { sensitivity: 'base' }))
}

export const mockTableViews = {
  list(tableKey: string): TableViewDto[] {
    assertTableKey(tableKey)
    const userId = requireUserId()
    return forUserTable(userId, tableKey).map(toDto)
  },

  create(body: CreateTableViewRequest): TableViewDto {
    assertTableKey(body.tableKey)
    assertColumnKeys(body.columnKeys)
    const name = body.name.trim()
    if (!name) {
      throw new ApiError(400, 'Name is required')
    }

    const userId = requireUserId()
    if (nameTaken(userId, body.tableKey, name)) {
      throw new ApiError(409, 'A view with this name already exists')
    }

    const siblings = forUserTable(userId, body.tableKey)
    const activate = body.activate ?? siblings.every((view) => !view.isActive)
    const created: StoredTableView = {
      id: crypto.randomUUID(),
      userId,
      tableKey: body.tableKey,
      name,
      columnKeys: [...body.columnKeys],
      isActive: activate,
    }

    if (activate) {
      deactivateOthers(userId, body.tableKey, created.id)
    }

    views.push(created)
    return toDto(created)
  },

  update(id: string, body: UpdateTableViewRequest): TableViewDto {
    const userId = requireUserId()
    const view = views.find((item) => item.id === id && item.userId === userId)
    if (!view) {
      throw new ApiError(404, 'View not found')
    }

    if (body.name !== undefined) {
      const name = body.name.trim()
      if (!name) {
        throw new ApiError(400, 'Name is required')
      }
      if (nameTaken(userId, view.tableKey, name, view.id)) {
        throw new ApiError(409, 'A view with this name already exists')
      }
      view.name = name
    }

    if (body.columnKeys !== undefined) {
      assertColumnKeys(body.columnKeys)
      view.columnKeys = [...body.columnKeys]
    }

    return toDto(view)
  },

  remove(id: string): void {
    const userId = requireUserId()
    const index = views.findIndex((item) => item.id === id && item.userId === userId)
    if (index < 0) {
      throw new ApiError(404, 'View not found')
    }

    const [removed] = views.splice(index, 1)
    if (removed?.isActive) {
      const remaining = forUserTable(userId, removed.tableKey)
      if (remaining[0]) {
        remaining[0].isActive = true
      }
    }
  },

  activate(id: string): TableViewDto {
    const userId = requireUserId()
    const view = views.find((item) => item.id === id && item.userId === userId)
    if (!view) {
      throw new ApiError(404, 'View not found')
    }

    deactivateOthers(userId, view.tableKey, view.id)
    view.isActive = true
    return toDto(view)
  },
}
