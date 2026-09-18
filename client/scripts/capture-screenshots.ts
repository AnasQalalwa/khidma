import { mkdir } from 'node:fs/promises'
import path from 'node:path'
import { fileURLToPath } from 'node:url'
import { chromium, type Page } from '@playwright/test'

const WIDTHS = [360, 768, 1280] as const
const VIEWPORT_HEIGHT = 900
const REQUEST_TITLE = 'Kitchen sink leaking under the cabinet'
const BASE_URL = (process.env.KHIDMA_BASE_URL ?? 'http://localhost:5173').replace(/\/$/, '')

const screenshotsDir = path.resolve(
  path.dirname(fileURLToPath(import.meta.url)),
  '../../docs/screenshots',
)

function requiredEnv(name: string): string {
  const value = process.env[name]
  if (!value) {
    throw new Error(`${name} is required.`)
  }

  return value
}

async function waitSettled(page: Page): Promise<void> {
  await page.waitForLoadState('domcontentloaded')
  await page.locator('text=Loading Khidma').waitFor({ state: 'hidden', timeout: 15000 }).catch(() => undefined)
  await page.waitForLoadState('networkidle')
}

async function captureWidths(page: Page, name: string, ready: () => Promise<void>): Promise<void> {
  for (const width of WIDTHS) {
    await page.setViewportSize({ width, height: VIEWPORT_HEIGHT })
    await ready()
    await waitSettled(page)
    const file = path.join(screenshotsDir, `${name}-${width}.png`)
    await page.screenshot({
      path: file,
      fullPage: true,
      scale: 'css',
      animations: 'disabled',
    })
    console.log(`wrote ${path.relative(process.cwd(), file)}`)
  }
}

async function login(page: Page, email: string, password: string): Promise<void> {
  await page.goto(`${BASE_URL}/login`)
  await waitSettled(page)
  await page.getByLabel('Email').fill(email)
  await page.getByRole('textbox', { name: 'Password' }).fill(password)
  await page.getByRole('button', { name: 'Login' }).click()
  await page.waitForURL((url) => !url.pathname.startsWith('/login'), { timeout: 15000 })
  await waitSettled(page)
}

async function logout(page: Page): Promise<void> {
  await page.getByRole('button', { name: 'Logout' }).first().click()
  await page.waitForURL('**/login', { timeout: 15000 })
  await waitSettled(page)
}

async function main(): Promise<void> {
  const customerEmail = requiredEnv('KHIDMA_CUSTOMER_EMAIL')
  const customerPassword = requiredEnv('KHIDMA_CUSTOMER_PASSWORD')
  const providerEmail = requiredEnv('KHIDMA_PROVIDER_EMAIL')
  const providerPassword = requiredEnv('KHIDMA_PROVIDER_PASSWORD')
  const adminEmail = requiredEnv('KHIDMA_ADMIN_EMAIL')
  const adminPassword = requiredEnv('KHIDMA_ADMIN_PASSWORD')

  await mkdir(screenshotsDir, { recursive: true })

  const browser = await chromium.launch({ headless: true })
  const context = await browser.newContext({
    baseURL: BASE_URL,
    deviceScaleFactor: 1,
    viewport: { width: 1280, height: VIEWPORT_HEIGHT },
    ignoreHTTPSErrors: true,
  })
  const page = await context.newPage()

  try {
    await page.goto(`${BASE_URL}/`)
    await captureWidths(page, 'home', async () => {
      await page.getByRole('heading', { name: /Trusted services/i }).waitFor()
    })

    await page.goto(`${BASE_URL}/catalog`)
    await captureWidths(page, 'catalog', async () => {
      await page.getByRole('heading', { name: /Find the right service/i }).waitFor()
      await page.getByText('Plumbing', { exact: true }).first().waitFor()
    })

    await login(page, customerEmail, customerPassword)

    await page.goto(`${BASE_URL}/customer/requests`)
    await waitSettled(page)
    await page.getByRole('link', { name: REQUEST_TITLE }).first().click()
    await waitSettled(page)
    await captureWidths(page, 'customer-request-detail', async () => {
      await page.getByRole('heading', { name: REQUEST_TITLE }).waitFor()
      await page.getByText('Accept offer').first().waitFor()
    })

    await page.goto(`${BASE_URL}/customer/bookings`)
    await waitSettled(page)
    await page.getByRole('heading', { name: 'Bookings' }).waitFor()
    await page.locator('article.request-card').first().getByRole('link', { name: 'View' }).click()
    await page.waitForURL(/\/customer\/bookings\/\d+/, { timeout: 15000 })
    await waitSettled(page)
    await captureWidths(page, 'booking-detail', async () => {
      await page.locator('.detail-grid').waitFor()
      await page.getByRole('heading', { level: 1 }).waitFor()
    })

    await logout(page)
    await login(page, providerEmail, providerPassword)

    await page.goto(`${BASE_URL}/provider/requests`)
    await captureWidths(page, 'provider-requests', async () => {
      await page.getByRole('heading', { name: 'Available requests' }).waitFor()
      await page.getByRole('link', { name: REQUEST_TITLE }).waitFor()
    })

    await logout(page)
    await login(page, adminEmail, adminPassword)

    await page.goto(`${BASE_URL}/admin/verifications`)
    await captureWidths(page, 'admin-verifications', async () => {
      await page.getByRole('heading', { name: 'Provider verification' }).waitFor()
      await page.getByText('Demo Provider Two').waitFor()
    })
  } finally {
    await context.close()
    await browser.close()
  }
}

await main()
