import { useCallback, useEffect, useMemo, useState, type ReactNode } from 'react'
import {
  getMe,
  login as loginRequest,
  logout as logoutRequest,
  register as registerRequest,
  type CurrentUser,
  type LoginPayload,
  type RegisterPayload,
} from '../api/auth'
import { ApiError, warmupAntiforgery } from '../api/client'
import { AuthContext, type AuthContextValue } from './AuthContext'

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<CurrentUser | null>(null)
  const [authResolved, setAuthResolved] = useState(false)
  const [bootError, setBootError] = useState<string | null>(null)

  const refreshUser = useCallback(async () => {
    try {
      const current = await getMe()
      setUser(current)
      return current
    } catch (error) {
      if (error instanceof ApiError && error.status === 401) {
        setUser(null)
        return null
      }

      throw error
    }
  }, [])

  useEffect(() => {
    let cancelled = false

    async function bootstrap() {
      await warmupAntiforgery()

      try {
        const current = await refreshUser()
        if (!cancelled) {
          setUser(current)
          setBootError(null)
        }
      } catch (error) {
        if (!cancelled) {
          const message =
            error instanceof ApiError
              ? error.message
              : 'Unable to reach the Khidma API.'
          setBootError(message)
          setUser(null)
        }
      } finally {
        if (!cancelled) {
          setAuthResolved(true)
        }
      }
    }

    void bootstrap()

    return () => {
      cancelled = true
    }
  }, [refreshUser])

  const login = useCallback(async (payload: LoginPayload) => {
    const current = await loginRequest(payload)
    setUser(current)
    return current
  }, [])

  const register = useCallback(async (payload: RegisterPayload) => {
    const current = await registerRequest(payload)
    setUser(current)
    return current
  }, [])

  const logout = useCallback(async () => {
    await logoutRequest()
    setUser(null)
  }, [])

  const value = useMemo<AuthContextValue>(
    () => ({
      user,
      loading: !authResolved,
      authenticated: user !== null,
      login,
      register,
      logout,
      refreshUser,
    }),
    [authResolved, login, logout, refreshUser, register, user],
  )

  if (!authResolved) {
    return (
      <div className="app-boot">
        <div className="boot-mark" aria-hidden="true" />
        <p>Loading Khidma…</p>
      </div>
    )
  }

  if (bootError) {
    return (
      <div className="app-boot app-boot-error">
        <div className="boot-mark" aria-hidden="true" />
        <p>
          Unable to reach the API. Confirm the backend is running on
          https://localhost:5001.
        </p>
      </div>
    )
  }

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}
