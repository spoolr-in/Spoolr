# Stage 1: Install dependencies
FROM node:20-alpine AS base

WORKDIR /app

# Copy package.json and package-lock.json
COPY package.json package-lock.json ./

# Install dependencies
RUN npm install --frozen-lockfile

# Stage 2: Build the Next.js application
FROM node:20-alpine AS build

WORKDIR /app

# Copy dependencies from base stage
COPY --from=base /app/node_modules ./node_modules

# Copy the rest of the application code
COPY . .

# Build the Next.js application
RUN npm run build

# Stage 3: Production image (runner)
FROM node:20-alpine AS runner

WORKDIR /app

# Set environment variables for Next.js
ENV NODE_ENV=production

# Copy necessary files from build stage
COPY --from=build /app/.next ./.next
COPY --from=build /app/public ./public
COPY --from=build /app/package.json ./package.json
COPY --from=build /app/node_modules ./node_modules

# Expose the port Next.js runs on
EXPOSE 3000

# Command to start the application
CMD ["npm", "start"]
