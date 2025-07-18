﻿using System;
using System.Linq;
using System.Collections.Generic;
using System.Windows.Forms;
using System.IO;
using SkiaSharp;
using Ephemera.NBagOfTricks;


namespace NProcessing.Script
{
    /// <summary>
    /// A port of the Processing PVector class.
    /// </summary>
    public class PVector // implements Serializable
    {
        static readonly Random _rand = new();
        protected double[]? _array;

        public double x;
        public double y;
        public double z;

        public PVector()
        {
        }

        public PVector(double x, double y, double z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }

        public PVector(double x, double y)
        {
            this.x = x;
            this.y = y;
            this.z = 0;
        }

        public PVector set(double x, double y, double z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
            return this;
        }

        public PVector set(double x, double y)
        {
            this.x = x;
            this.y = y;
            return this;
        }

        public PVector set(PVector v)
        {
            x = v.x;
            y = v.y;
            z = v.z;
            return this;
        }

        public PVector set(double[] source)
        {
            if (source.Length >= 2)
            {
                x = source[0];
                y = source[1];
            }

            if (source.Length >= 3)
            {
                z = source[2];
            }

            return this;
        }

        static public PVector random2D()
        {
            double angle = _rand.NextDouble() * Math.PI * 2;
            return new PVector(Math.Cos(angle), Math.Sin(angle), 0);
        }

        static public PVector random2D(PVector target)
        {
            target.set(random2D());
            return target;
        }

        static public PVector random3D(PVector target)
        {
            double angle;
            double vz;

            angle = _rand.NextDouble() * Math.PI * 2;
            vz = _rand.NextDouble() * 2 - 1;

            double vx = Math.Sqrt(1 - vz * vz) * Math.Cos(angle);
            double vy = Math.Sqrt(1 - vz * vz) * Math.Sin(angle);

            if (target is null)
            {
                target = new PVector(vx, vy, vz);
                //target.normalize(); // Should be unnecessary
            }
            else
            {
                target.set(vx, vy, vz);
            }

            return target;
        }

        public PVector copy()
        {
            return new PVector(x, y, z);
        }

        //[Obsolete()]
        public PVector get() => copy();

        public double[] get(double[] target)
        {
            if (target is null)
            {
                return new double[] { x, y, z };
            }

            if (target.Length >= 2)
            {
                target[0] = x;
                target[1] = y;
            }

            if (target.Length >= 3)
            {
                target[2] = z;
            }

            return target;
        }

        public double mag()
        {
            return Math.Sqrt(x * x + y * y + z * z);
        }

        public double magSq()
        {
            return (x * x + y * y + z * z);
        }

        public PVector add(PVector v)
        {
            x += v.x;
            y += v.y;
            z += v.z;
            return this;
        }

        public PVector add(double x, double y)
        {
            this.x += x;
            this.y += y;
            return this;
        }

        public PVector add(double x, double y, double z)
        {
            this.x += x;
            this.y += y;
            this.z += z;
            return this;
        }

        static public PVector add(PVector v1, PVector v2)
        {
            return new PVector(v1.x + v2.x, v1.y + v2.y, v1.z + v2.z);
        }

        static public PVector add(PVector v1, PVector v2, PVector target)
        {
            target.set(add(v1, v2));

            return target;
        }

        public PVector sub(PVector v)
        {
            x -= v.x;
            y -= v.y;
            z -= v.z;
            return this;
        }

        public PVector sub(double x, double y)
        {
            this.x -= x;
            this.y -= y;
            return this;
        }

        public PVector sub(double x, double y, double z)
        {
            this.x -= x;
            this.y -= y;
            this.z -= z;
            return this;
        }

        static public PVector sub(PVector v1, PVector v2)
        {
            return new PVector(v1.x - v2.x, v1.y - v2.y, v1.z - v2.z);
        }

        static public PVector sub(PVector v1, PVector v2, PVector target)
        {
            target.set(sub(v1, v2));

            return target;
        }

        public PVector mult(double n)
        {
            x *= n;
            y *= n;
            z *= n;
            return this;
        }

        static public PVector mult(PVector v, double n)
        {
            return new PVector(v.x * n, v.y * n, v.z * n);
        }

        static public PVector mult(PVector v, double n, PVector target)
        {
            target.set(mult(v, n));

            return target;
        }

        public PVector div(double n)
        {
            x /= n;
            y /= n;
            z /= n;
            return this;
        }

        static public PVector div(PVector v, double n)
        {
            return new PVector(v.x / n, v.y / n, v.z / n);
        }

        static public PVector div(PVector v, double n, PVector target)
        {
            target.set(div(v, n));
            return target;
        }

        public double dist(PVector v)
        {
            double dx = x - v.x;
            double dy = y - v.y;
            double dz = z - v.z;
            return Math.Sqrt(dx * dx + dy * dy + dz * dz);
        }

        static public double dist(PVector v1, PVector v2)
        {
            double dx = v1.x - v2.x;
            double dy = v1.y - v2.y;
            double dz = v1.z - v2.z;
            return Math.Sqrt(dx * dx + dy * dy + dz * dz);
        }

        public double dot(PVector v)
        {
            return x * v.x + y * v.y + z * v.z;
        }

        public double dot(double x, double y, double z)
        {
            return this.x * x + this.y * y + this.z * z;
        }

        static public double dot(PVector v1, PVector v2)
        {
            return v1.x * v2.x + v1.y * v2.y + v1.z * v2.z;
        }

        public PVector cross(PVector v)
        {
            double crossX = y * v.z - v.y * z;
            double crossY = z * v.x - v.z * x;
            double crossZ = x * v.y - v.x * y;

            return new PVector(crossX, crossY, crossZ);
        }

        public PVector cross(PVector v, PVector target)
        {
            double crossX = y * v.z - v.y * z;
            double crossY = z * v.x - v.z * x;
            double crossZ = x * v.y - v.x * y;

            target.set(crossX, crossY, crossZ);

            return target;
        }

        static public PVector cross(PVector v1, PVector v2, PVector target)
        {
            double crossX = v1.y * v2.z - v2.y * v1.z;
            double crossY = v1.z * v2.x - v2.z * v1.x;
            double crossZ = v1.x * v2.y - v2.x * v1.y;

            target.set(crossX, crossY, crossZ);

            return target;
        }

        public PVector normalize()
        {
            double m = mag();
            if (m != 0 && m != 1)
            {
                div(m);
            }

            return this;
        }

        public PVector normalize(PVector target)
        {
            target ??= new PVector();
            double m = mag();
            if (m > 0)
            {
                target.set(x / m, y / m, z / m);
            }
            else
            {
                target.set(x, y, z);
            }
            return target;
        }

        public PVector limit(double max)
        {
            if (magSq() > max * max)
            {
                normalize();
                mult(max);
            }
            return this;
        }

        public PVector setMag(double len)
        {
            normalize();
            mult(len);
            return this;
        }

        public PVector setMag(PVector target, double len)
        {
            target = normalize(target);
            target.mult(len);
            return target;
        }

        public double heading()
        {
            double angle = Math.Atan2(y, x);
            return angle;
        }

        [Obsolete("Use heading().")]
        public double heading2D()
        {
            return heading();
        }

        public PVector rotate(double theta)
        {
            double temp = x;
            // Might need to check for rounding errors like with angleBetween function?
            x *= (Math.Cos(theta) - y * Math.Sin(theta));
            y = temp * (Math.Sin(theta) + y * Math.Cos(theta));

            return this;
        }

        double lerp(double start, double stop, double amt)
        {
            return start + (stop - start) * amt;
        }

        public PVector lerp(PVector v, double amt)
        {
            x = lerp(x, v.x, amt);
            y = lerp(y, v.y, amt);
            z = lerp(z, v.z, amt);
            return this;
        }

        public static PVector lerp(PVector v1, PVector v2, double amt)
        {
            PVector v = v1.copy();
            v.lerp(v2, amt);
            return v;
        }

        public PVector lerp(double x, double y, double z, double amt)
        {
            this.x = lerp(this.x, x, amt);
            this.y = lerp(this.y, y, amt);
            this.z = lerp(this.z, z, amt);
            return this;
        }

        static public double angleBetween(PVector v1, PVector v2)
        {
            // We get NaN if we pass in a zero vector which can cause problems
            // Zero seems like a reasonable angle between a (0, 0, 0) vector and something else.
            if (v1.x == 0 && v1.y == 0 && v1.z == 0) return 0.0f;
            if (v2.x == 0 && v2.y == 0 && v2.z == 0) return 0.0f;

            double dot = v1.x * v2.x + v1.y * v2.y + v1.z * v2.z;
            double v1mag = Math.Sqrt(v1.x * v1.x + v1.y * v1.y + v1.z * v1.z);
            double v2mag = Math.Sqrt(v2.x * v2.x + v2.y * v2.y + v2.z * v2.z);
            // This should be a number between -1 and 1, since it's "normalized"
            double amt = dot / (v1mag * v2mag);
            // But if it's not due to rounding error, then we need to fix it
            // http://code.google.com/p/processing/issues/detail?id=340
            // Otherwise if outside the range, acos() will return NaN
            // http://www.cppreference.com/wiki/c/math/acos
            if (amt <= -1)
            {
                return ScriptCore.PI;
            }
            else if (amt >= 1)
            {
                // http://code.google.com/p/processing/issues/detail?id=435
                return 0;
            }

            return Math.Acos(amt);
        }

        public override string ToString()
        {
            return $"x:{x} y:{y} z:{z}";
        }

        public double[] array()
        {
            _array ??= new double[3];
            _array[0] = x;
            _array[1] = y;
            _array[2] = z;

            return _array;
        }

        public override bool Equals(object? obj)
        {
            bool ret = false;
            if (obj is not null && obj is PVector vector)
            {
                PVector p = vector;
                ret = x == p.x && y == p.y && z == p.z;
            }
            return ret;
        }

        public override int GetHashCode()
        {
            int hash = 0;

            foreach (double f in new double[] { x, y, z })
            {
                foreach (byte b in BitConverter.GetBytes(f))
                {
                    hash <<= 4;
                    hash &= b;
                }
            }

            return hash;
        }
    }
}